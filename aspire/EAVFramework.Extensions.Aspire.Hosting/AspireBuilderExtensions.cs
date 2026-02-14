using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using EAVFramework.Configuration;
using EAVFramework.Extensions.Aspire.Hosting.Database;
using EAVFW.Extensions.SecurityModel;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
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
using Aspire.Hosting.JavaScript;

#pragma warning disable ASPIREINTERACTION001 // Interaction service is in preview

namespace EAVFramework.Extensions.Aspire.Hosting
{
    /// <summary>
    /// Annotation that stores a SQL Server database resource reference on a project resource,
    /// allowing WithEAVModel to discover the database without requiring it as a parameter.
    /// </summary>
    public record SqlServerDatabaseAnnotation(
        IResourceBuilder<SqlServerDatabaseResource> Database) : IResourceAnnotation;

    /// <summary>
    /// Annotation that enables hot-reload mode for an EAVFW app,
    /// starting a Next.js dev server as a child resource in Aspire.
    /// </summary>
    public record EAVFWHotReloadAnnotation() : IResourceAnnotation;

    /// <summary>
    /// Annotation that configures npm link to use local @eavfw/* packages
    /// from the EAVFW monorepo instead of registry-published versions.
    /// </summary>
    public record EAVFWNpmLinkAnnotation(string MonorepoRoot) : IResourceAnnotation;

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

            public Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
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
                return Task.CompletedTask;
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
        /// Configures npm link to use local @eavfw/* packages from the EAVFW monorepo
        /// instead of registry-published versions. This enables testing framework changes
        /// directly in scaffolded projects without publishing to npm first.
        /// </summary>
        /// <param name="builder">The project resource builder returned by <see cref="AddEAVFWApp{TProject}"/>.</param>
        /// <param name="monorepoRoot">Path to the EAVFW monorepo root (relative to the AppHost project directory, or absolute).</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithNpmLink(
            this IResourceBuilder<ProjectResource> builder, string monorepoRoot)
        {
            var metadata = builder.Resource.GetProjectMetadata();
            var appHostDir = Path.GetDirectoryName(metadata.ProjectPath);
            var resolvedPath = Path.GetFullPath(Path.Combine(appHostDir, monorepoRoot));

            builder.WithAnnotation(new EAVFWNpmLinkAnnotation(resolvedPath),
                ResourceAnnotationMutationBehavior.Replace);
            return builder;
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
            builder.Services.TryAddLifecycleHook<EAVFWStartupNotificationHook>();
            var project = builder.AddProject<TProject>(name, launchProfile)
                  .WithUrlForEndpoint("https", u => u.DisplayText = "EAV Dev Portal");

            var workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, "../.."));

            var metadata = project.Resource.GetProjectMetadata();

            var srcPath = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), "src");
            if (!string.IsNullOrEmpty(npmBuildCommand) && Directory.Exists(srcPath))
            {
                var hash = BuildEAVFWAppsLifecycleHook.CreateMd5ForFolder(srcPath);
                var oldhash = File.Exists(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) ?
                    File.ReadAllText(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) : null;

                // Add npm install resource
                var npmInstall = builder.AddResource(
                    new EavNpmInstallResource(name + "-npm-install", workingDirectory) { Project = project.Resource })
                    .WithInitialState(new CustomResourceSnapshot
                    {
                        ResourceType = "NPM Install",
                        State = new ResourceStateSnapshot("Pending", KnownResourceStateStyles.Info),
                        Properties = []
                    });
                npmInstall.WithParentRelationship(project);

                // Add build resource (depends on npm install)
                var buildSkipped = hash == oldhash;
                var build = builder.AddResource(
                    new EavBuildResource(name + "-eav-build", "npm", workingDirectory, "run", npmBuildCommand) { Project = project.Resource })
                    .WithInitialState(new CustomResourceSnapshot
                    {
                        ResourceType = "EAV Build",
                        State = new ResourceStateSnapshot(buildSkipped ? KnownResourceStates.Finished : "Pending", buildSkipped ? KnownResourceStateStyles.Success : KnownResourceStateStyles.Info),
                        ExitCode = buildSkipped ? 0 : null,
                        Properties = []
                    });
                build.WithParentRelationship(project);
                build.WaitForCompletion(npmInstall);

                project.WithAnnotation(new EAVFWBuildAnnotation { ProjectResource = project.Resource });

                project.WaitForCompletion(build);
            }



            // Auto-enable hot-reload via configuration fallback
            if (string.Equals(builder.Configuration["EAVFW_HOTRELOAD"], "true", StringComparison.OrdinalIgnoreCase))
            {
                project.WithHotReload();
            }

            return project;
        }

        /// <summary>
        /// Enables hot-reload mode for an EAVFW app by starting a Next.js dev server
        /// as a child resource in the Aspire dashboard.
        /// </summary>
        /// <param name="builder">The project resource builder returned by <see cref="AddEAVFWApp{TProject}"/>.</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithHotReload(
            this IResourceBuilder<ProjectResource> builder)
        {
            // Idempotent — skip if already configured
            if (builder.Resource.TryGetLastAnnotation<EAVFWHotReloadAnnotation>(out _))
                return builder;

            builder.WithAnnotation(new EAVFWHotReloadAnnotation(), ResourceAnnotationMutationBehavior.Replace);

            var metadata = builder.Resource.GetProjectMetadata();
            var portalDir = Path.GetDirectoryName(metadata.ProjectPath);
            var appBuilder = builder.ApplicationBuilder;
            var name = builder.Resource.Name;

            var nextDev = appBuilder.AddJavaScriptApp(name + "-next-dev", portalDir, "dev")
                .WithNpm(install: false)
                .WithHttpEndpoint(env: "PORT")
                .WithEnvironment("NODE_ENV", "development")
                .WithEnvironment("BROWSER", "none")
                .WithEnvironment(ctx =>
                {
                    var endpoint = builder.GetEndpoint("http");
                    ctx.EnvironmentVariables["NEXT_PUBLIC_API_BASE_URL"] =
                        ReferenceExpression.Create($"{endpoint}/api");
                    ctx.EnvironmentVariables["NEXT_PUBLIC_BASE_URL"] =
                        ReferenceExpression.Create($"{endpoint}/");
                })
                .WithParentRelationship(builder)
                .WaitFor(builder)
                .WithUrlForEndpoint("http", u => u.DisplayText = "EAV Dev Portal (Hot Reload)");

            // Tell Portal to allow CORS from dev server
            builder.WithEnvironment(ctx =>
            {
                var devEndpoint = nextDev.GetEndpoint("http");
                ctx.EnvironmentVariables["EAVFW_CORS_ORIGINS"] = devEndpoint;
            });

            return builder;
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
                InitialEmail = administratorEmail, InitialIdentity = initialAdministratorUserId, SystemUsersTableName = systemUsersTableName, Schema = schema, InitialUsername = Username, UserPrincipalName = Username.Replace(" ", "")
            }, ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }
        public static IResourceBuilder<T> WithSigninUrl<T>(this IResourceBuilder<T> builder, IResourceBuilder<EAVFWModelProjectResource> model, IResourceBuilder<ProjectResource> project = null)
            where T : Resource
        {
            model.WithAnnotation(new CreateSigninUrlAnnotation(builder.Resource, project?.Resource ?? builder.Resource as IResource));
            return builder;
        }
        public static IResourceBuilder<EAVFWModelProjectResource> WithSinginToken<TContext, TIdentity, TSignin>(

            this IResourceBuilder<EAVFWModelProjectResource> builder, Action<IServiceCollection> services = null, Action<SqlServerDbContextOptionsBuilder> sql = null)
            where TContext : DynamicContext
            where TIdentity : DynamicEntity, IIdentity
            where TSignin : DynamicEntity, ISigninRecord, new()
        {
            builder.ApplicationBuilder.Services.TryAddLifecycleHook<PublishEAVFWProjectLifecycleHook>();
            builder.WithAnnotation<CreateSigninTokenAnnotation>(new CreateSigninTokenAnnotation<TContext, TIdentity, TSignin>(builder.Resource)
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

        /// <summary>
        /// Configures the project resource to wait for the EAV model to complete and establishes a parent-child relationship.
        /// </summary>
        /// <typeparam name="T">The project resource type.</typeparam>
        /// <param name="builder">The project resource builder.</param>
        /// <param name="modelBuilder">The EAV model resource builder to wait for.</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithEAVModel(
            this IResourceBuilder<ProjectResource> builder,
            IResourceBuilder<EAVFWModelProjectResource> modelBuilder,
            IResourceBuilder<SqlServerDatabaseResource> database)
        {
            // Add database reference
            builder.WithReference(database, "ApplicationDB");

            // Wait for the model to complete before starting the project
            builder.WaitForCompletion(modelBuilder);

            // Establish parent-child relationship (project is parent of model)
            modelBuilder.WithParentRelationship(builder);

            return builder;
        }

        /// <summary>
        /// Creates and configures an EAV model for the project resource with full setup including database publishing and signin tokens.
        /// </summary>
        /// <typeparam name="TModel">The model project that contains the manifest.g.json file.</typeparam>
        /// <typeparam name="TContext">The database context type.</typeparam>
        /// <typeparam name="TIdentity">The identity type for signin tokens.</typeparam>
        /// <typeparam name="TSignin">The signin type for signin tokens.</typeparam>
        /// <param name="builder">The project resource builder.</param>
        /// <param name="modelName">The name for the model resource.</param>
        /// <param name="database">The database resource to publish to.</param>
        /// <param name="initialEmail">The initial user email address.</param>
        /// <param name="initialIdentity">The initial user identity GUID.</param>
        /// <param name="initialUsername">The initial username.</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithEAVModel<TModel, TContext, TIdentity, TSignin>(
            this IResourceBuilder<ProjectResource> builder,
            string modelName,
            IResourceBuilder<SqlServerDatabaseResource> database,
            string initialEmail,
            Guid initialIdentity,
            string initialUsername)
            where TModel : IProjectMetadata, new()
            where TContext : DynamicContext
            where TIdentity : DynamicEntity, IIdentity
            where TSignin : DynamicEntity, ISigninRecord, new()
        {
            // Create the EAV model with full configuration
            var modelBuilder = builder.ApplicationBuilder
                .AddEAVFWModel<TModel>(modelName)
                .PublishTo(database, initialEmail, initialIdentity, initialUsername)
                .WithSinginToken<TContext, TIdentity, TSignin>();

            // Use the simple overload to set up relationships
            return builder.WithEAVModel(modelBuilder, database);
        }

        /// <summary>
        /// Configures SMTP settings for the project using a mail server container resource.
        /// Automatically extracts host and port from the mail server's SMTP endpoint.
        /// </summary>
        /// <param name="builder">The project resource builder.</param>
        /// <param name="mailServer">The mail server container resource (e.g., Mailpit, MailHog).</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithMailServer(
            this IResourceBuilder<ProjectResource> builder,
            IResourceBuilder<ContainerResource> mailServer)
        {
            // Configure SMTP using endpoint references
            // GetEndpoint returns a ReferenceExpression that resolves at runtime
            builder
                .WithEnvironment("Smtp__Host", mailServer.GetEndpoint("smtp").Property(EndpointProperty.Host))
                .WithEnvironment("Smtp__Port", mailServer.GetEndpoint("smtp").Property(EndpointProperty.Port))
                .WithEnvironment("Smtp__Password", "")
                .WithEnvironment("Smtp__Username", "")
                .WithEnvironment("Smtp__EnableSsl", "false");

            return builder;
        }

        /// <summary>
        /// Adds a Mailpit container for local SMTP testing with web UI.
        /// Mailpit provides a local SMTP server with a web interface for viewing sent emails during development.
        /// </summary>
        /// <param name="builder">The distributed application builder.</param>
        /// <param name="name">The name of the mail server resource. Defaults to "mail-server".</param>
        /// <param name="httpPort">The host port for the web UI. Defaults to null (random port) to avoid conflicts when running multiple projects.</param>
        /// <param name="smtpPort">The host port for SMTP. Defaults to null (random port) to avoid conflicts when running multiple projects.</param>
        /// <returns>The container resource builder for further configuration.</returns>
        public static IResourceBuilder<ContainerResource> AddMailpit(
            this IDistributedApplicationBuilder builder,
            string name = "mail-server",
            int? httpPort = null,
            int? smtpPort = null)
        {
            return builder
                .AddContainer(name, "axllent/mailpit")
                .WithHttpEndpoint(httpPort, 8025, name: "http")
                .WithEndpoint(smtpPort, 1025, name: "smtp")
                .WithLifetime(ContainerLifetime.Persistent)
                .WithUrlForEndpoint("http", u => u.DisplayText = "Read mail here");
        }

        /// <summary>
        /// Adds a Mailpit container and configures the project to use it for SMTP.
        /// This is a convenience method that combines AddMailpit() and WithMailServer() into one call.
        /// Uses random ports by default to avoid conflicts when running multiple projects in parallel.
        /// </summary>
        /// <param name="builder">The project resource builder.</param>
        /// <param name="name">The name of the mail server resource. Defaults to "mail-server".</param>
        /// <param name="httpPort">The host port for the web UI. Defaults to null (random port).</param>
        /// <param name="smtpPort">The host port for SMTP. Defaults to null (random port).</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithMailPit(
            this IResourceBuilder<ProjectResource> builder,
            string name = "mail-server",
            int? httpPort = null,
            int? smtpPort = null)
        {
            // Add the Mailpit container to the application
            var mailServer = builder.ApplicationBuilder.AddMailpit(name, httpPort, smtpPort);

            // Configure this project to use the mail server
            return builder.WithMailServer(mailServer);
        }

        /// <summary>
        /// Adds a SQL Server instance with database, persistent data volume, and BACPAC restore support.
        /// This is a convenience method that wraps all the SQL Server setup into one call, similar to <see cref="WithMailPit"/>.
        /// The database reference is stored as an annotation so <see cref="WithEAVModel{TModel,TContext,TIdentity,TSignin}(IResourceBuilder{ProjectResource},string,string,Guid,string)"/>
        /// can discover it automatically without passing it explicitly.
        /// </summary>
        /// <param name="builder">The project resource builder.</param>
        /// <param name="existingDb">An optional existing database resource. If null, a new SQL Server instance is created with sensible defaults.</param>
        /// <param name="configureSqlServer">Optional callback to further configure the SQL Server resource (e.g., add DbGate, custom volumes).</param>
        /// <param name="serverName">The name for the SQL Server resource. Defaults to "sqlserver".</param>
        /// <param name="databaseName">The name for the database resource. Defaults to "sql-db".</param>
        /// <param name="defaultPassword">The default SQL Server password. Defaults to a strong password.</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithSqlServerDB(
            this IResourceBuilder<ProjectResource> builder,
            IResourceBuilder<SqlServerDatabaseResource> existingDb = null,
            Action<IResourceBuilder<SqlServerServerResource>> configureSqlServer = null,
            string serverName = "sqlserver",
            string databaseName = "sql-db",
            string defaultPassword = "Your_strong_password123!")
        {
            IResourceBuilder<SqlServerDatabaseResource> db;

            if (existingDb != null)
            {
                db = existingDb;
            }
            else
            {
                var prefix = builder.Resource.Name;
                var sqlPass = builder.ApplicationBuilder.AddParameter("sql-server-password", defaultPassword, secret: true);

                var sqlServer = builder.ApplicationBuilder
                    .AddSqlServer(serverName, sqlPass)
                    .WithDataVolume($"{prefix}-sql-data")
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithRestoreBacpacCommand(defaultDatabaseName: databaseName);

                // Allow consumers to add DbGate, custom configuration, etc.
                configureSqlServer?.Invoke(sqlServer);

                db = sqlServer.AddDatabase(databaseName);
            }

            // Store the database reference so WithEAVModel can discover it
            builder.WithAnnotation(new SqlServerDatabaseAnnotation(db), ResourceAnnotationMutationBehavior.Replace);

            return builder;
        }

        /// <summary>
        /// Creates and configures an EAV model for the project resource, automatically discovering the SQL Server database
        /// from a prior <see cref="WithSqlServerDB"/> call.
        /// </summary>
        /// <typeparam name="TModel">The model project that contains the manifest.g.json file.</typeparam>
        /// <typeparam name="TContext">The database context type.</typeparam>
        /// <typeparam name="TIdentity">The identity type for signin tokens.</typeparam>
        /// <typeparam name="TSignin">The signin type for signin tokens.</typeparam>
        /// <param name="builder">The project resource builder.</param>
        /// <param name="modelName">The name for the model resource.</param>
        /// <param name="initialEmail">The initial user email address.</param>
        /// <param name="initialIdentity">The initial user identity GUID.</param>
        /// <param name="initialUsername">The initial username.</param>
        /// <returns>The project resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> WithEAVModel<TModel, TContext, TIdentity, TSignin>(
            this IResourceBuilder<ProjectResource> builder,
            string modelName,
            string initialEmail,
            Guid initialIdentity,
            string initialUsername)
            where TModel : IProjectMetadata, new()
            where TContext : DynamicContext
            where TIdentity : DynamicEntity, IIdentity
            where TSignin : DynamicEntity, ISigninRecord, new()
        {
            if (!builder.Resource.TryGetLastAnnotation<SqlServerDatabaseAnnotation>(out var dbAnnotation))
            {
                throw new InvalidOperationException(
                    "No SQL Server database configured. Call WithSqlServerDB() before WithEAVModel(), " +
                    "or use the overload that accepts an IResourceBuilder<SqlServerDatabaseResource>.");
            }

            return builder.WithEAVModel<TModel, TContext, TIdentity, TSignin>(
                modelName, dbAnnotation.Database, initialEmail, initialIdentity, initialUsername);
        }


        /// <summary>
        /// Forwards configuration values prefixed with the project name as environment variables.
        /// <para>
        /// Uses flat configuration format with double underscores as section delimiters.
        /// Property names must match C# property names (PascalCase) for ASP.NET Core binding to work.
        /// </para>
        /// <para>
        /// Format: "SCL_PORTAL__SectionName__PropertyName" = "value"
        /// </para>
        /// <para>
        /// Example: "SCL_PORTAL__CrmFeatureFlags__EnableCrmPolling" = "true"
        /// </para>
        /// <para>
        /// This will be forwarded to the service as: "CrmFeatureFlags__EnableCrmPolling" = "true"
        /// </para>
        /// <para>
        /// Note: ASP.NET Core configuration is case-insensitive but does NOT remove underscores,
        /// so "ENABLE_CRM_POLLING" will NOT match property "EnableCrmPolling".
        /// </para>
        /// </summary>
        /// <typeparam name="TProject">The project type (e.g., Projects.SCL_Portal).</typeparam>
        /// <param name="builder">The resource builder.</param>
        /// <returns>The resource builder for chaining.</returns>
        public static IResourceBuilder<ProjectResource> ForwardEnvironmentVariables<TProject>(
            this IResourceBuilder<ProjectResource> builder)
        {
            // Extract project name from type (e.g., Projects.SCL_Portal -> "SCL_Portal")
            var projectTypeName = typeof(TProject).Name;

            // Get the configuration
            var configuration = builder.ApplicationBuilder.Configuration;

            // Check for flat format with both original case and uppercase
            // (SCL_Portal__Key__SubKey or SCL_PORTAL__KEY__SUBKEY)
            var flatPrefixes = new[]
            {
                $"{projectTypeName}__",
                $"{projectTypeName.ToUpperInvariant()}__"
            };

            foreach (var flatPrefix in flatPrefixes)
            {
                // Iterate through all configuration keys
                foreach (var section in configuration.GetChildren())
                {
                    ProcessConfigurationSection(builder, section, flatPrefix);
                }
            }

            return builder;
        }

        private static void ProcessConfigurationSection(
            IResourceBuilder<ProjectResource> builder,
            IConfigurationSection section,
            string prefix)
        {
            var key = section.Path;

            // Check if this key starts with our prefix
            if (key != null && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = section.Value;

                if (!string.IsNullOrEmpty(value))
                {
                    // Strip the prefix and forward
                    var envVarName = key.Substring(prefix.Length);
                    builder.WithEnvironment(envVarName, value);
                }
            }

            // Recursively process child sections
            foreach (var child in section.GetChildren())
            {
                ProcessConfigurationSection(builder, child, prefix);
            }
        }

    }

    /// <summary>
    /// Lifecycle hook that shows a startup notification in the Aspire dashboard
    /// informing users that first-time startup may take several minutes.
    /// </summary>
    internal sealed class EAVFWStartupNotificationHook(
        IServiceProvider serviceProvider,
        ILogger<EAVFWStartupNotificationHook> logger) : IDistributedApplicationLifecycleHook
    {
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            // Fire-and-forget: notification is informational and must never block startup.
            // PromptNotificationAsync can hang if the interaction service is not fully connected.
            _ = Task.Run(async () =>
            {
                try
                {
                    var interactionService = serviceProvider.GetRequiredService<IInteractionService>();

                    if (interactionService.IsAvailable)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(5));

                        await interactionService.PromptNotificationAsync(
                            title: "EAVFW First-Time Setup",
                            message: "First-time startup can take up to 5 minutes while SQL Server is pulled, " +
                                     "npm packages are installed, and the application is built. " +
                                     "Subsequent starts will be much faster.",
                            options: new NotificationInteractionOptions
                            {
                                Intent = MessageIntent.Information
                            });
                    }
                }
                catch (Exception ex)
                {
                    // Non-critical — don't let notification failure block startup
                    logger.LogDebug(ex, "Could not show startup notification");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }
    }

}