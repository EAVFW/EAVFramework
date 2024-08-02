using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.Retry;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    /// <summary>
    /// The lifecycle hook that waits for all dependencies to be "running" before starting resources. 
    /// 
    /// See <see cref="NeedsAnnotation"/> for configuring annotations that waits.
    /// </summary>
    /// <param name="executionContext"></param>
    /// <param name="resourceNotificationService"></param>
    public class NeedsLifecycleHook(DistributedApplicationExecutionContext executionContext,
          ResourceNotificationService resourceNotificationService) :
          IDistributedApplicationLifecycleHook,
          IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts = new();


        /// <summary>
        /// Before starting resources we will loop over all resources to find those with <see cref="NeedsAnnotation"/> annotations.
        /// </summary>
        /// <param name="appModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            // We don't need to execute any of this logic in publish mode
            if (executionContext.IsPublishMode)
            {
                return Task.CompletedTask;
            }

            var waitingResources = ScanForWaitOnAnnotations(resourceNotificationService, appModel);

            _ = Task.Run(()=>LongRunningWaiterImplementationAsync(resourceNotificationService, waitingResources), cancellationToken);

            return Task.CompletedTask;
        }

        private async Task LongRunningWaiterImplementationAsync(
            ResourceNotificationService resourceNotificationService,
            ConcurrentDictionary<IResource, ConcurrentDictionary<NeedsAnnotation, TaskCompletionSource>> waitingResources)
        {
           
                var stoppingToken = _cts.Token;

                // These states are terminal but we need a better way to detect that
                static bool IsKnownTerminalState(CustomResourceSnapshot snapshot) =>
                    snapshot.State == "FailedToStart" ||
                    snapshot.State == "Exited" ||
                    snapshot.ExitCode is not null;

                // Watch for global resource state changes
                await foreach (var resourceEvent in resourceNotificationService.WatchAsync(stoppingToken))
                {
                    if (waitingResources.TryGetValue(resourceEvent.Resource, out var pendingAnnotations))
                    {
                        foreach (var (waitOn, tcs) in pendingAnnotations)
                        {
                            if (waitOn.States is string[] states && states.Contains(resourceEvent.Snapshot.State?.Text, StringComparer.Ordinal))
                            {
                                pendingAnnotations.TryRemove(waitOn, out _);

                                _ = DoTheHealthCheck(resourceEvent, tcs);
                            }
                            else if (waitOn.WaitUntilCompleted)
                            {
                                if (IsKnownTerminalState(resourceEvent.Snapshot))
                                {
                                    pendingAnnotations.TryRemove(waitOn, out _);

                                    _ = DoTheHealthCheck(resourceEvent, tcs);
                                }
                            }
                            else if (waitOn.States is null)
                            {
                                if (resourceEvent.Snapshot.State?.Text == "Running")
                                {
                                    pendingAnnotations.TryRemove(waitOn, out _);

                                    _ = DoTheHealthCheck(resourceEvent, tcs);
                                }
                                else if (IsKnownTerminalState(resourceEvent.Snapshot))
                                {
                                    pendingAnnotations.TryRemove(waitOn, out _);

                                    tcs.TrySetException(new Exception($"Dependency {waitOn.Resource.Name} failed to start"));
                                }
                            }
                        }
                    }
                }
            
        }

        private static ConcurrentDictionary<IResource, ConcurrentDictionary<NeedsAnnotation, TaskCompletionSource>> ScanForWaitOnAnnotations(ResourceNotificationService resourceNotificationService, DistributedApplicationModel appModel)
        {
            // The global list of resources being waited on
            var waitingResources = new ConcurrentDictionary<IResource, ConcurrentDictionary<NeedsAnnotation, TaskCompletionSource>>();

            // For each resource, add an environment callback that waits for dependencies to be running
            foreach (var r in appModel.Resources)
            {
                var resourcesToWaitOn = r.Annotations.OfType<NeedsAnnotation>().ToLookup(a => a.Resource);

                if (resourcesToWaitOn.Count == 0)
                {
                    continue;
                }

                // Abuse the environment callback to wait for dependencies to be running

                r.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
                {
                    var dependencies = new List<Task>();

                    // Find connection strings and endpoint references and get the resource they point to
                    foreach (var group in resourcesToWaitOn)
                    {
                        var resource = group.Key;

                        // REVIEW: This logic does not handle cycles in the dependency graph (that would result in a deadlock)

                        // Don't wait for yourself
                        if (resource != r && resource is not null)
                        {
                            var pendingAnnotations = waitingResources.GetOrAdd(resource, _ => new());

                            foreach (var waitOn in group)
                            {
                                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                                async Task Wait()
                                {
                                    context.Logger?.LogInformation("Waiting for {Resource}.", waitOn.Resource.Name);

                                    await tcs.Task;

                                    context.Logger?.LogInformation("Waiting for {Resource} completed.", waitOn.Resource.Name);
                                }

                                pendingAnnotations[waitOn] = tcs;

                                dependencies.Add(Wait());
                            }
                        }
                    }

                    await resourceNotificationService.PublishUpdateAsync(r, s => s with
                    {
                        State = new("Waiting", KnownResourceStateStyles.Info)
                    });

                    await Task.WhenAll(dependencies).WaitAsync(context.CancellationToken);
                }));
            }

            return waitingResources;
        }

        private async Task DoTheHealthCheck(ResourceEvent resourceEvent, TaskCompletionSource tcs)
        {
            var resource = resourceEvent.Resource;

            // REVIEW: Right now, every resource does an independent health check, we could instead cache
            // the health check result and reuse it for all resources that depend on the same resource


            HealthCheckAnnotation? healthCheckAnnotation = null;

            // Find the relevant health check annotation. If the resource has a parent, walk up the tree
            // until we find the health check annotation.
            while (true)
            {
                // If we find a health check annotation, break out of the loop
                if (resource.TryGetLastAnnotation(out healthCheckAnnotation))
                {
                    break;
                }

                // If the resource has a parent, walk up the tree
                if (resource is IResourceWithParent parent)
                {
                    resource = parent.Parent;
                }
                else
                {
                    break;
                }
            }

            Func<CancellationToken, ValueTask>? operation = null;

            if (healthCheckAnnotation?.HealthCheckFactory is { } factory)
            {
                IHealthCheck? check;

                try
                {
                    // TODO: Do need to pass a cancellation token here?
                    check = await factory(resource, default);

                    if (check is not null)
                    {
                        var context = new HealthCheckContext()
                        {
                            Registration = new HealthCheckRegistration("", check, HealthStatus.Unhealthy, [])
                        };

                        operation = async (cancellationToken) =>
                        {
                            var result = await check.CheckHealthAsync(context, cancellationToken);

                            if (result.Exception is not null)
                            {
                                ExceptionDispatchInfo.Throw(result.Exception);
                            }

                            if (result.Status != HealthStatus.Healthy)
                            {
                                throw new Exception("Health check failed");
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);

                    return;
                }
            }

            try
            {
                if (operation is not null)
                {
                    var pipeline = CreateResiliencyPipeline();

                    await pipeline.ExecuteAsync(operation);
                }

                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private static ResiliencePipeline CreateResiliencyPipeline()
        {
            var retryUntilCancelled = new RetryStrategyOptions()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 5,
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(30)
            };

            return new ResiliencePipelineBuilder().AddRetry(retryUntilCancelled).Build();
        }

        public ValueTask DisposeAsync()
        {
            _cts.Cancel();
            return default;
        }
    }

   
}
