using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using EAVFramework.Configuration;
using EAVFW.Extensions.SecurityModel;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public sealed class CertificateResource(string name) : Resource(name), IResourceWithEnvironment
    {
        public string Value { get; set; }

        public List<X509Extension> Extensions { get; set; } = new List<X509Extension>();
    }

    public record TargetKeyVaultResourceAnnotation(AzureKeyVaultResource resource) : IResourceAnnotation
    {
    }
    internal static class PathNormalizer
    {
        public static string NormalizePathForCurrentPlatform(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // Fix slashes
            path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

            return Path.GetFullPath(path);
        }
    }
    public static class AspireBuilderExtensions
    {



        public static IResourceBuilder<CertificateResource> PublishTo(this IResourceBuilder<CertificateResource> builder, IResourceBuilder<AzureKeyVaultResource> target)
        {

            builder.ApplicationBuilder.Services.TryAddLifecycleHook(PublishToKeyVaultHook.Factory);
            builder.WithAnnotation(new TargetKeyVaultResourceAnnotation(target.Resource), ResourceAnnotationMutationBehavior.Replace);
            return builder;

        }

        /// <summary>
        /// Writes the message to the specified resource's logs.
        /// </summary>
        private sealed class PublishToKeyVaultHook(
            DistributedApplicationExecutionContext executionContext,
            ResourceNotificationService resourceNotificationService,
            ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook, IAsyncDisposable
        {

            private readonly CancellationTokenSource _cts = new();

            public static PublishToKeyVaultHook Factory(IServiceProvider sp)
            {
                return new PublishToKeyVaultHook(
                    sp.GetRequiredService<DistributedApplicationExecutionContext>(),
                    sp.GetRequiredService<ResourceNotificationService>(),
                    sp.GetRequiredService<ResourceLoggerService>());
            }
            public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
            {
                // We don't need to execute any of this logic in publish mode

                var stoppingToken = _cts.Token;

                var waitingResources = new ConcurrentDictionary<IResource, ConcurrentDictionary<TargetKeyVaultResourceAnnotation, TaskCompletionSource>>();

                // For each resource, add an environment callback that waits for dependencies to be running
                foreach (var r in appModel.Resources)
                {
                    var resourcesToWaitOn = r.Annotations.OfType<TargetKeyVaultResourceAnnotation>().ToLookup(a => a.resource);

                    if (resourcesToWaitOn.Count == 0)
                    {
                        continue;
                    }

                    //  var dependencies = new List<Task>();

                    foreach (var group in resourcesToWaitOn)
                    {
                        var resource = group.Key;

                        var pendingAnnotations = waitingResources.GetOrAdd(resource, _ => new());

                        foreach (var waitOn in group)
                        {
                            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                            //async Task Wait()
                            //{
                            //    //  context.Logger?.LogInformation("Waiting for {Resource}.", waitOn.Resource.Name);

                            //    await tcs.Task;

                            //    //  context.Logger?.LogInformation("Waiting for {Resource} completed.", waitOn.Resource.Name);
                            //}

                            pendingAnnotations[waitOn] = tcs;

                            //  dependencies.Add(Wait());
                        }

                    }
                    await resourceNotificationService.PublishUpdateAsync(r, s => s with
                    {
                        State = new("Waiting on keyvault", KnownResourceStateStyles.Info)
                    });
                    //   await Task.WhenAll(dependencies).WaitAsync(cancellationToken);

                }

                var _ = Task.Run(async () =>
                {

                    await foreach (var resourceEvent in resourceNotificationService.WatchAsync(stoppingToken))
                    {
                        if (waitingResources.TryGetValue(resourceEvent.Resource, out var pendingAnnotations))
                        {
                            foreach (var (waitOn, tcs) in pendingAnnotations)
                            {

                                var snapshot = resourceEvent.Snapshot;
                                if (snapshot.State.Text == KnownResourceStates.Running)
                                {
                                    var logger = loggerService.GetLogger(waitOn.resource);
                                    var vaultUri = await waitOn.resource.VaultUri.GetValueAsync();

                                    var credentialSetting = "test";

                                    TokenCredential credential = credentialSetting switch
                                    {
                                        "AzureCli" => new AzureCliCredential(),
                                        "AzurePowerShell" => new AzurePowerShellCredential(),
                                        "VisualStudio" => new VisualStudioCredential(),
                                        "VisualStudioCode" => new VisualStudioCodeCredential(),
                                        "AzureDeveloperCli" => new AzureDeveloperCliCredential(),
                                        "InteractiveBrowser" => new InteractiveBrowserCredential(),
                                        _ => new DefaultAzureCredential(new DefaultAzureCredentialOptions()
                                        {
                                            ExcludeManagedIdentityCredential = true,
                                            ExcludeWorkloadIdentityCredential = true,
                                            ExcludeAzurePowerShellCredential = true,
                                            CredentialProcessTimeout = TimeSpan.FromSeconds(15)
                                        })
                                    };

                                    if (credential.GetType() == typeof(DefaultAzureCredential))
                                    {
                                        logger.LogInformation(
                                            "Using DefaultAzureCredential for provisioning. This may not work in all environments. " +
                                            "See https://aka.ms/azsdk/net/identity/default-azure-credential for more information.");
                                    }
                                    else
                                    {
                                        logger.LogInformation("Using {credentialType} for provisioning.", credential.GetType().Name);
                                    }

                                    var certClient = new CertificateClient(new Uri(vaultUri), credential);

                                    foreach (var cert in appModel.Resources.OfType<CertificateResource>()
                                        .Where(r => r.TryGetAnnotationsOfType<TargetKeyVaultResourceAnnotation>(out var sources) && sources.Any(s => s.resource == resourceEvent.Resource)))
                                    {
                                        try
                                        {
                                            var a = certClient.GetPropertiesOfCertificatesAsync().ToBlockingEnumerable()
                                            .Any(c => c.Name == cert.Name);

                                            if (!a || (await certClient.GetCertificateAsync(cert.Name)).Value.Properties.ExpiresOn < DateTimeOffset.UtcNow.AddDays(14))
                                            {
                                                await certClient.ImportCertificateAsync(
                                                    new ImportCertificateOptions(cert.Name, Convert.FromBase64String(cert.Value))
                                                    {
                                                        Enabled = true,
                                                        Tags = { { "CreatedBy", "Aspire" } }
                                                    }, cancellationToken);


                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError(ex, "Failed to import certificate {certName}", cert.Name);
                                            throw;
                                        }
                                    }

                                    pendingAnnotations.TryRemove(waitOn, out var _);

                                }
                            }


                        }
                    }


                }, cancellationToken);


            }

            public ValueTask DisposeAsync()
            {
                _cts.Cancel();
                return default;
            }
        }


        public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<CertificateResource> parameter) where T : IResourceWithEnvironment
        {

            builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, () =>
            {
                if (parameter.Resource.Value == null)
                    return "";

                return parameter.Resource.Value;

            }));


            return builder;
        }

        public static IResourceBuilder<CertificateResource> WithX509Exension(this IResourceBuilder<CertificateResource> builder, X509Extension extension)
        {
            builder.Resource.Extensions.Add(extension);
            return builder;

        }
        public static IResourceBuilder<CertificateResource> UseForSigning(this IResourceBuilder<CertificateResource> builder)
        {

            return builder.WithX509Exension(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

        }
        public static IResourceBuilder<CertificateResource> UseForEncryption(this IResourceBuilder<CertificateResource> builder)
        {

            return builder.WithX509Exension(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: true));

        }
        public static IResourceBuilder<ProjectResource> WithHostname(this IResourceBuilder<ProjectResource> builder, string host)
        {

            builder.ApplicationBuilder.Services.AddLifecycleHook((sp) => new EndpointUpdaterHook(
              sp.GetRequiredService<ResourceNotificationService>(),
                 sp.GetRequiredService<ResourceLoggerService>(),
                 builder.Resource, host));

            return builder;
        }
        private sealed class EndpointUpdaterHook(
           ResourceNotificationService resourceNotificationService,
           ResourceLoggerService loggerService,
           ProjectResource project, string host) : IDistributedApplicationLifecycleHook
        {

            public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
            {
                var _ = Task.Run(async () =>
                {

                    await foreach (var update in resourceNotificationService.WatchAsync(cancellationToken))
                    {
                        try
                        {
                            if (update.Resource == project && update.Snapshot.Urls.Any(x => new Uri(x.Url).Host == "localhost"))
                            {
                                var endpoints = project.Annotations.OfType<EndpointAnnotation>().ToImmutableArray();
                                
                                await resourceNotificationService.PublishUpdateAsync(project,
                                        state => state = state with
                                        {
                                            Urls = state.Urls.Select(url => url with
                                            {
                                                Url = url.Url.Replace("localhost", host)
                                            }).ToImmutableArray()

                                        });
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                });


            }
        }


        public static IResourceBuilder<CertificateResource> AddCertificate(this IDistributedApplicationBuilder builder, string subject)
        {
            var resource = new CertificateResource(subject);



            var state = new CustomResourceSnapshot()
            {
                ResourceType = "Certificate",
                // hide parameters by default
                State = KnownResourceStates.Starting,
                Properties = [
                    new("parameter.encryption", "true"),
                  //  new(CustomResourceKnownProperties.Source,  $"Parameters:{subject}")
                ]
            };

            //try
            //{
            //    state = state with { Properties = [.. state.Properties, new("Value", resource.Value)] };
            //}
            //catch (DistributedApplicationException ex)
            //{
            //    state = state with
            //    {
            //        State = new ResourceStateSnapshot("Configuration missing", KnownResourceStateStyles.Error),
            //        Properties = [.. state.Properties, new("Value", ex.Message)]
            //    };

            //    builder.Services.AddLifecycleHook((sp) => new WriteParameterLogsHook(
            //        sp.GetRequiredService<ResourceLoggerService>(),
            //        subject,
            //        ex.Message));
            //}

            builder.Services.AddLifecycleHook((sp) => new CreateCertificateHook(
                sp.GetRequiredService<ResourceNotificationService>(),
                   sp.GetRequiredService<ResourceLoggerService>(),
                   resource));

            return builder.AddResource(resource)
                          .WithInitialState(state);

        }

        /// <summary>
        /// Writes the message to the specified resource's logs.
        /// </summary>
        private sealed class WriteParameterLogsHook(ResourceLoggerService loggerService, string resourceName, string message) : IDistributedApplicationLifecycleHook
        {
            public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
            {
                loggerService.GetLogger(resourceName).LogError(message);

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Writes the message to the specified resource's logs.
        /// </summary>
        private sealed class CreateCertificateHook(
            ResourceNotificationService resourceNotificationService,
            ResourceLoggerService loggerService,
            CertificateResource certificateResource) : IDistributedApplicationLifecycleHook
        {

            public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
            {

                using var algorithm = RSA.Create(keySizeInBits: 2048);

                var request = new CertificateRequest("CN=" + certificateResource.Name, algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                foreach (var ext in certificateResource.Extensions)
                {
                    request.CertificateExtensions.Add(ext);
                }

                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));

                // Note: setting the friendly name is not supported on Unix machines (including Linux and macOS). 
                // To ensure an exception is not thrown by the property setter, an OS runtime check is used here. 
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //  certificate.FriendlyName = "OpenIddict Server Encryption Certificate";
                }

                // Note: CertificateRequest.CreateSelfSigned() doesn't mark the key set associated with the certificate 
                // as "persisted", which eventually prevents X509Store.Add() from correctly storing the private key. 
                // To work around this issue, the certificate payload is manually exported and imported back 
                // into a new X509Certificate2 instance specifying the X509KeyStorageFlags.PersistKeySet flag. 
                var data = certificate.Export(X509ContentType.Pfx);
                certificateResource.Value = Convert.ToBase64String(data);


                await resourceNotificationService.PublishUpdateAsync(certificateResource, s => s with
                {
                    State = new(KnownResourceStates.Running, KnownResourceStateStyles.Info),
                    Properties = [.. s.Properties, new("Value", certificateResource.Value)],
                    EnvironmentVariables = [.. s.EnvironmentVariables, new(certificateResource.Name, certificateResource.Value, true)]
                });





            }
        }


        /// <summary>
        /// Adds a EAVFW Model Project resource to the application.
        /// </summary>
        /// <typeparam name="TProject">The model project that contains the manifest.g.json file</typeparam>
        /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/> instance to add the model project to.</param>
        /// <param name="name">Name of the resource.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> AddEAVFWModel<TProject>(this IDistributedApplicationBuilder builder, string name)
            where TProject : IProjectMetadata, new()
        {

            return builder.AddEAVFWModel(name).WithAnnotation(new TProject());


        }




        public static IResourceBuilder<ProjectResource> AddEAVFWApp<TProject>(this IDistributedApplicationBuilder builder,
            string name, string npmBuildCommand, string launchProfile)
             where TProject : IProjectMetadata, new()
        {
            builder.Services.TryAddLifecycleHook<BuildEAVFWAppsLifecycleHook>();
            var project = builder.AddProject<TProject>(name, launchProfile);

            var workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, "../.."));

            var metadata = project.Resource.GetProjectMetadata();
           
            if (!string.IsNullOrEmpty(npmBuildCommand))
            {
                var hash = BuildEAVFWAppsLifecycleHook.CreateMd5ForFolder(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), "src"));
                var oldhash = File.Exists(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) ?
                    File.ReadAllText(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) : null;


                var build = builder.AddResource(new EavBuildResource(name + "-eav", "npm", workingDirectory, "run", npmBuildCommand) { Project = project.Resource })
                    .WithInitialState(new CustomResourceSnapshot { ResourceType = "EAV Build",
                        State = new ResourceStateSnapshot(hash != oldhash ? "Building" : KnownResourceStates.Finished, KnownResourceStateStyles.Success),
                        Properties = [] });

                if (hash == oldhash)
                    build.WithAnnotation(new NeedsCompletedAnnotation());

                project.WithAnnotation(new EAVFWBuildAnnotation { ProjectResource = project.Resource })
                .Needs(build);
            }



            return project;
        }


        /// <summary>
        /// Adds a EAVFW Model Project resource to the application.
        /// </summary>
        /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/> instance to add the model project to.</param>
        /// <param name="name">Name of the resource.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> AddEAVFWModel(this IDistributedApplicationBuilder builder, string name)
        {
            var resource = new EAVFWModelProjectResource(name);


            var state = new CustomResourceSnapshot()
            {
                ResourceType = "Data Model",

                // hide parameters by default
                State = new ResourceStateSnapshot("Generating", KnownResourceStateStyles.Info),
                Urls = [new UrlSnapshot("manifest.json", $"file://manifest.json", false)],
                Properties = []

            };


            return builder.AddResource(resource)
                .ExcludeFromManifest()
                .WithInitialState(state)
               .WithAnnotation(HealthCheckAnnotation.Create(cs => new AspireEAVFWHealthCheck(cs)));
        }



        /// <summary>
        /// Restore a database from a remote .bak file
        /// </summary>
        /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the EAVFW Model Resource</param>
        /// <param name="bakpath">Path to the .dacpac file.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> FromBackup(this IResourceBuilder<EAVFWModelProjectResource> builder, string bakpath)
        {
            throw new NotImplementedException("Not done yet");
        }

        /// <summary>
        /// Publishes the EAVFW Model to the target <see cref="SqlServerDatabaseResource"/>.
        /// </summary>
        /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the EAVFW Model Project that should be published to a target database project</param>
        /// <param name="target">An <see cref="IResourceBuilder{T}"/> representing the target <see cref="SqlServerDatabaseResource"/> to publish the model to.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> PublishTo(
            this IResourceBuilder<EAVFWModelProjectResource> builder,
            IResourceBuilder<SqlServerDatabaseResource> target,
            string administratorEmail, Guid initialAdministratorUserId, string Username, string schema = "dbo", string systemUsersTableName = "SystemUsers"
            )
        {
            builder.ApplicationBuilder.Services.TryAddLifecycleHook<PublishEAVFWProjectLifecycleHook>();
            builder.WithAnnotation(new TargetDatabaseResourceAnnotation(target.Resource.Name, target.Resource)
            {
                InitialEmail = administratorEmail, InitialIdentity = initialAdministratorUserId, SystemUsersTableName = systemUsersTableName, Schema = schema, InitialUsername = Username, UserPrincipalName = Username.Replace(" ","")
            }, ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }
        public static IResourceBuilder<T> WithSigninUrl<T>(this IResourceBuilder<T> builder, IResourceBuilder<EAVFWModelProjectResource> model, IResourceBuilder<ProjectResource> project=null)
            where T:Resource
        {
            model.WithAnnotation(new CreateSigninUrlAnnotation(builder.Resource,project?.Resource??builder.Resource as IResource));
            return builder;
        }
        public static IResourceBuilder<EAVFWModelProjectResource> WithSinginToken<TContext, TIdentity, TSignin>(
          
            this IResourceBuilder<EAVFWModelProjectResource> builder, Action<IServiceCollection> services = null, Action<SqlServerDbContextOptionsBuilder> sql=null)
            where TContext : DynamicContext
            where TIdentity : DynamicEntity, IIdentity
            where TSignin : DynamicEntity, ISigninRecord, new()
        {
            builder.ApplicationBuilder.Services.TryAddLifecycleHook<PublishEAVFWProjectLifecycleHook>();
            builder.WithAnnotation< CreateSigninTokenAnnotation>(new CreateSigninTokenAnnotation<TContext, TIdentity, TSignin>(builder.Resource)
            {
                ServiceCollectionExtender = services,
                SqlExtender = sql,
            }, ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }


        /// <summary>
        /// Mark the model project to include the NetTopologySuite library when generating the database schemas
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>

        public static IResourceBuilder<EAVFWModelProjectResource> WithNetTopologySuite(
        this IResourceBuilder<EAVFWModelProjectResource> builder)
        {

            builder.WithAnnotation(new NetTopologySuiteAnnotation(), ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }




    }

}