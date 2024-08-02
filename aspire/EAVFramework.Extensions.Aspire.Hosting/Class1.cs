using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using EAVFramework.Infrastructure.HealthChecks;
using EAVFW.Extensions.Manifest.SDK;
using EAVFW.Extensions.Manifest.SDK.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFramework.Extensions.Aspire.Hosting
{

    public record EAVFWModelMetadataAnnotation(string ModelPath) : IResourceAnnotation
    {
    }
    public record TargetDatabaseResourceAnnotation(string TargetDatabaseResourceName, SqlServerDatabaseResource TargetDatabaseResource) : IResourceAnnotation
    {
    }

    public record NetTopologySuiteAnnotation() : IResourceAnnotation
    {
    }


    public sealed class EAVFWModelProjectResource(string name) : Resource(name)
    {
        public string GetModelPath()
        {
            var projectMetadata = Annotations.OfType<IProjectMetadata>().FirstOrDefault();
            if (projectMetadata != null)
            {
                var projectPath = projectMetadata.ProjectPath;

                return projectPath;
            }

            var dacpacMetadata = Annotations.OfType<EAVFWModelMetadataAnnotation>().FirstOrDefault();
            if (dacpacMetadata != null)
            {
                return dacpacMetadata.ModelPath;
            }

            throw new InvalidOperationException($"Unable to locate SQL Server Database project package for resource {Name}.");
        }
    }


    public class PublishEAVFWProjectLifecycleHook : IDistributedApplicationLifecycleHook
    {
        private readonly ResourceLoggerService _resourceLoggerService;
        private readonly ResourceNotificationService _resourceNotificationService;

        public PublishEAVFWProjectLifecycleHook(ResourceLoggerService resourceLoggerService,
            ResourceNotificationService resourceNotificationService)
        {
            _resourceLoggerService = resourceLoggerService ?? throw new ArgumentNullException(nameof(resourceLoggerService));
            _resourceNotificationService = resourceNotificationService ?? throw new ArgumentNullException(nameof(resourceNotificationService));
        }

        public async Task BeforeStartAsync(DistributedApplicationModel application, CancellationToken cancellationToken = default)
        {
            var _ = Task.Factory.StartNew(async () =>
            {

                int retryCount = 5;
                int delay = 1000; // Initial delay in milliseconds

                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        foreach (var sqlProject in application.Resources.OfType<EAVFWModelProjectResource>())
                        {

                            var logger = _resourceLoggerService.GetLogger(sqlProject);

                            var modelProjectPath = sqlProject.GetModelPath();
                            if (!File.Exists(modelProjectPath))
                            {
                                logger.LogError("SQL Server Database project package not found at path {DacpacPath}.", modelProjectPath);
                                await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                                    state => state with { State = new ResourceStateSnapshot("Failed", KnownResourceStateStyles.Error) });
                                continue;
                            }

                            var targetDatabaseResourceName = sqlProject.Annotations.OfType<TargetDatabaseResourceAnnotation>().Single().TargetDatabaseResourceName;
                            var targetDatabaseResource = application.Resources.OfType<SqlServerDatabaseResource>().Single(r => r.Name == targetDatabaseResourceName);
                            var targetSQLServerResource = targetDatabaseResource.Parent;
                            var serverConnectionString = await targetSQLServerResource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
                            var connectionString = await targetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);

                            await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                                state => state with { State = new ResourceStateSnapshot("Creating DB " + targetDatabaseResource.DatabaseName, KnownResourceStateStyles.Info) });


                            try
                            {

                                using var conn = new SqlConnection(serverConnectionString);

                                await conn.OpenAsync();

                                SqlCommand cmd = conn.CreateCommand();
                                cmd.CommandText = $"""
                                       IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{targetDatabaseResource.DatabaseName}')
                                       BEGIN
                                         CREATE DATABASE [{targetDatabaseResource.DatabaseName}];
                                         ALTER DATABASE [{targetDatabaseResource.DatabaseName}] SET RECOVERY SIMPLE;
                                       END
                                       """;
                                await cmd.ExecuteNonQueryAsync();


                                await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                                    state => state with { State = new ResourceStateSnapshot($"{targetDatabaseResource.DatabaseName} created", KnownResourceStateStyles.Info) });

                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to create db");

                                await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                                    state => state with { State = new ResourceStateSnapshot("Failed", KnownResourceStateStyles.Error) });

                                throw;
                            }

                            await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                              state => state with { State = new ResourceStateSnapshot("Creating Migrations", KnownResourceStateStyles.Info) });

                            try
                            {

                                var variablegenerator = new SQLClientParameterGenerator();
                                var migrator = new SQLMigrationGenerator(variablegenerator, new ManifestPermissionGenerator(variablegenerator));

                                var sqls = await migrator.GenerateSQL(Path.GetDirectoryName(modelProjectPath), true, "SystemUsers",
                                    o =>
                                    {
                                        o.UseNetTopologySuite();
                                    });

                                var replacements = new Dictionary<string, string>
                                {
                                    ["DBName"] = targetDatabaseResource.DatabaseName,
                                    ["DBSchema"] = "dbo",
                                    ["SystemAdminSecurityGroupId"] = "1b714972-8d0a-4feb-b166-08d93c6ae328",
                                    ["UserGuid"] = "A0461D73-E979-449C-B24F-A3B3D6711D61",
                                    ["UserName"] = "Poul Kjeldager",
                                    ["UserEmail"] = "poul@kjeldager.com",
                                    ["UserPrincipalName"] = "PoulKjeldagerSorensen"

                                };

                                using (SqlConnection conn = new SqlConnection(connectionString))
                                {
                                    var files = new[] { sqls.SQL, sqls.Permissions };
                                    await conn.OpenAsync();
                                    foreach (var file in files)
                                    {
                                        var cmdText = variablegenerator.DoReplacements(file, replacements);

                                        foreach (var sql in cmdText.Split("GO"))
                                        {
                                            using var cmd = conn.CreateCommand();

                                            cmd.CommandText = sql.Trim();
                                            //  await context.Context.Database.ExecuteSqlRawAsync(sql);

                                            if (!string.IsNullOrEmpty(cmd.CommandText))
                                            {
                                                var r = await cmd.ExecuteNonQueryAsync();
                                                Console.WriteLine("Rows changed: " + r);
                                            }
                                        }






                                    }

                                }


                                await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                              state => state with { State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success) });


                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to create migrations");

                                await _resourceNotificationService.PublishUpdateAsync(sqlProject,
                                    state => state with { State = new ResourceStateSnapshot("Failed", KnownResourceStateStyles.Error) });

                                throw;
                            }


                        }


                    }
                    catch (Exception ex)
                    {
                        if (i == retryCount - 1)
                        {
                           
                        }
                        else
                        {
                            await Task.Delay(delay);
                            delay *= 2; // Exponential backoff
                        }
                    }
                }

            }, cancellationToken);

        }

        public Task AfterResourcesCreatedAsync(DistributedApplicationModel application, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }


    public static class EAVFWAspireBuilderExtensions
    {
        /// <summary>
        /// Adds a EAVFW Model Project resource to the application.
        /// </summary>
        /// <typeparam name="TProject">Type that represents the project that produces the .dacpac file.</typeparam>
        /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/> instance to add the SQL Server Database project to.</param>
        /// <param name="name">Name of the resource.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> AddEAVFWModel<TProject>(this IDistributedApplicationBuilder builder, string name)
            where TProject : IProjectMetadata, new()
        {


            var resource = new EAVFWModelProjectResource(name);

            return builder.AddResource(resource)
                          .WithAnnotation(new TProject());
        }



        /// <summary>
        /// Adds a EAVFW Model Project resource to the application.
        /// </summary>
        /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/> instance to add the SQL Server Database project to.</param>
        /// <param name="name">Name of the resource.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> AddEAVFWModel(this IDistributedApplicationBuilder builder, string name)
        {
            var resource = new EAVFWModelProjectResource(name);


            return builder.AddResource(resource)
                .WithAnnotation(EAVHealthCheckAnnotation.Create(cs =>
            {

                return new EAVDBHealthCheck(cs);
            }));
        }
        /// <summary>
        /// An annotation that associates a health check factory with a resource
        /// </summary>
        /// <param name="healthCheckFactory">A function that creates the health check</param>
        public class EAVHealthCheckAnnotation(Func<IResource, CancellationToken, Task<IHealthCheck?>> healthCheckFactory) : IResourceAnnotation
        {
            public Func<IResource, CancellationToken, Task<IHealthCheck?>> HealthCheckFactory { get; } = healthCheckFactory;

            public static EAVHealthCheckAnnotation Create(Func<EAVFWModelProjectResource,IHealthCheck> connectionStringFactory)
            {
                return new(async (resource, token) =>
                {
                    if (resource is EAVFWModelProjectResource eavmodel)
                    {
                        return connectionStringFactory(eavmodel);
                    }

                    return null;
                  
                });
            }
        }

        public class EAVDBHealthCheck : IHealthCheck
        {
            private EAVFWModelProjectResource _cs;

            public EAVDBHealthCheck(EAVFWModelProjectResource cs)
            {
                _cs = cs;
            }

            public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            {
                var targetDatabaseResource= _cs.Annotations.OfType<TargetDatabaseResourceAnnotation>().Single().TargetDatabaseResource;
                var targetSQLServerResource = targetDatabaseResource.Parent;
                var serverConnectionString = await targetSQLServerResource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
                var connectionString = await targetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);


                return HealthCheckResult.Unhealthy();
            }
        }
        /// <summary>
        /// Specifies the path to the .bak file.
        /// </summary>
        /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the SQL Server Database project.</param>
        /// <param name="bakpath">Path to the .dacpac file.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> FromBackup(this IResourceBuilder<EAVFWModelProjectResource> builder, string bakpath)
        {
            throw new NotImplementedException("Not done yet");

            //if (!Path.IsPathRooted(dacpacPath))
            //{
            //    dacpacPath = Path.Combine(builder.ApplicationBuilder.AppHostDirectory, dacpacPath);
            //}

            //return builder.WithAnnotation(new DacpacMetadataAnnotation(dacpacPath));
        }

        /// <summary>
        /// Publishes the EAVFW Model to the target <see cref="SqlServerDatabaseResource"/>.
        /// </summary>
        /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the SQL Server Database project to publish.</param>
        /// <param name="target">An <see cref="IResourceBuilder{T}"/> representing the target <see cref="SqlServerDatabaseResource"/> to publish the SQL Server Database project to.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> PublishTo(
            this IResourceBuilder<EAVFWModelProjectResource> builder, IResourceBuilder<SqlServerDatabaseResource> target)
        {
            builder.ApplicationBuilder.Services.TryAddLifecycleHook<PublishEAVFWProjectLifecycleHook>();
            builder.WithAnnotation(new TargetDatabaseResourceAnnotation(target.Resource.Name,target.Resource), ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }


        public static IResourceBuilder<EAVFWModelProjectResource> WithNetTopologySuite(
        this IResourceBuilder<EAVFWModelProjectResource> builder)
        {

            builder.WithAnnotation(new NetTopologySuiteAnnotation(), ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }




        ///// <summary>
        ///// A resource that represents a specified .NET project.
        ///// </summary>
        ///// <param name="name">The name of the resource.</param>
        //public class EAVResource : Resource, IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery
        //{
        //    public EAVResource(string name) : base(name)
        //    {
        //        //this.Annotations.Add(new ManifestPublishingCallbackAnnotation(ctx => {



        //        //}));


        //        //this.Annotations.Add(new ResourceSnapshotAnnotation(new CustomResourceSnapshot {
        //        //     ResourceType = "EAVFramework",
        //        //      Properties = new System.Collections.Immutable.ImmutableArray<ResourcePropertySnapshot>(),
        //        //       State = new ResourceStateSnapshot("Running",KnownResourceStateStyles.Success)


        //        //}));

        //    }
        //}

        //internal sealed class EAVResourceLifecycleHook(ResourceNotificationService notificationService, ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook, IAsyncDisposable
        //{
        //    private readonly CancellationTokenSource _tokenSource = new();

        //    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        //    {
        //        foreach (var resource in appModel.Resources.OfType<EAVResource>())
        //        {

        //            var states = new[] { "Starting", "Running", "Finished", "Uploading", "Downloading", "Processing", "Provisioning" };
        //            var stateStyles = new[] { "info", "success", "warning", "error" };

        //            var logger = loggerService.GetLogger(resource);

        //            Task.Run(async () =>
        //            {
        //                var seconds = Random.Shared.Next(2, 12);

        //                logger.LogInformation("Starting test resource {ResourceName} with update interval {Interval} seconds", resource.Name, seconds);

        //                await notificationService.PublishUpdateAsync(resource, state => state with
        //                {
        //                    Properties = [.. state.Properties, new("Interval", seconds.ToString(CultureInfo.InvariantCulture))]
        //                });

        //                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

        //                while (await timer.WaitForNextTickAsync(_tokenSource.Token))
        //                {
        //                    //resource.Annotations.OfType<EndpointReferenceAnnotation>();
        //                    var randomState = states[Random.Shared.Next(0, states.Length)];
        //                    var randomStyle = stateStyles[Random.Shared.Next(0, stateStyles.Length)];
        //                    await notificationService.PublishUpdateAsync(resource, state => state with
        //                    {
        //                        State = new(randomState, randomStyle)
        //                    });

        //                    logger.LogInformation("Test resource {ResourceName} is now in state {State}", resource.Name, randomState);
        //                    return;
        //                }
        //            },
        //            cancellationToken);
        //        }

        //        return Task.CompletedTask;
        //    }

        //    public ValueTask DisposeAsync()
        //    {
        //        _tokenSource.Cancel();
        //        return default;
        //    }
        //}


        //public static class EAVResourceExtensions
        //{

        //    public static IResourceBuilder<EAVResource> AddEAVFWResource(this IDistributedApplicationBuilder builder, IResourceBuilder<IResourceWithConnectionString> server, IResourceBuilder<IResourceWithConnectionString> db, string name)
        //    {
        //        builder.Services.TryAddLifecycleHook<EAVResourceLifecycleHook>();

        //        var endpointReferenceAnnotation = db.Resource.Annotations;

        //        var resource = db.Resource;
        //        //  connectionName ??= resource.Name;

        //        var rb = builder.AddResource(new EAVResource(name))
        //            .WithEnvironment(c =>
        //            {
        //                if (server.Resource is IResourceWithEndpoints endpoints)
        //                {
        //                    var a = endpoints.GetEndpoints();
        //                }
        //            })
        //            .WithEnvironment(context =>
        //            {

        //                if (db.Resource is IResourceWithEndpoints endpoints)
        //                {
        //                    var a = endpoints.GetEndpoints();
        //                }

        //                var connectionStringName = resource.ConnectionStringEnvironmentVariable ?? $"ConnectionStrings__{resource.Name}";

        //                context.EnvironmentVariables[connectionStringName] = new ConnectionStringReference(resource, false);



        //            })
        //                      .WithInitialState(new()
        //                      {
        //                          ResourceType = "EAVFW",
        //                          State = "Starting",
        //                          Properties = [
        //                              new("P1", "P2"),
        //                       new(CustomResourceKnownProperties.Source, "Custom")
        //                          ]
        //                      });
        //        // .ExcludeFromManifest();

        //        return rb;
        //    }
        //}


    }

}