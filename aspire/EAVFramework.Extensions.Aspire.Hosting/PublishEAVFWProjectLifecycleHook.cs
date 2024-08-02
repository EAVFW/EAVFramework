using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using EAVFramework.Infrastructure.HealthChecks;
using EAVFW.Extensions.Manifest.SDK;
using EAVFW.Extensions.Manifest.SDK.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    public record DatabaseCreatedAnnotation() : IResourceAnnotation
    {
        public int Attempt { get; set; } = 1;
        public bool Success { get; set; }
    }

    public record  EAVFWMigrationdAnnotation() : IResourceAnnotation
    {
        public bool Success { get; set; }
        public int Attempt { get; set; } = 1;
    }

    /// <summary>
    /// The lifecycle hook that monitors the EAVFW Model Project resources and publishes updates to the resource state.
    /// </summary>

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
            var _ = Task.Run(async () =>
            {


                foreach (var modelResource in application.Resources.OfType<EAVFWModelProjectResource>())
                {
                    int retryCount = 10;
                    int delay = 1000; // Initial delay in milliseconds

                    for (int i = 0; i < retryCount; i++)
                    {
                        try
                        {

                            var logger = _resourceLoggerService.GetLogger(modelResource);

                            var modelProjectPath = modelResource.GetModelPath();
                            
                            var targetDatabaseResourceName = modelResource.Annotations.OfType<TargetDatabaseResourceAnnotation>().Single().TargetDatabaseResourceName;
                            var targetDatabaseResource = application.Resources.OfType<SqlServerDatabaseResource>().Single(r => r.Name == targetDatabaseResourceName);
                            var targetSQLServerResource = targetDatabaseResource.Parent;
                           

                            if(!modelResource.TryGetLastAnnotation(out DatabaseCreatedAnnotation createdatabaseannovation))
                            {
                                modelResource.Annotations.Add(createdatabaseannovation = new DatabaseCreatedAnnotation());
                            }

                            if (!createdatabaseannovation.Success)
                            {


                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                    state => state with { State = new ResourceStateSnapshot($"Creating DB ${targetDatabaseResource.DatabaseName} - Attempt ${createdatabaseannovation.Attempt}", KnownResourceStateStyles.Info) });

                                try
                                {
                                    var serverConnectionString = await targetSQLServerResource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);


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



                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot($"{targetDatabaseResource.DatabaseName} was created", KnownResourceStateStyles.Info) });

                                    createdatabaseannovation.Success = true;
                                }
                                catch (InvalidOperationException invalid)
                                {
                                    createdatabaseannovation.Attempt++;
                                    logger.LogWarning(invalid, "Transient error, properly due to endpoints not up yet. We are backing off and trying again.");
                                    throw;
                                }catch (SqlException sqlexception)
                                {
                                    createdatabaseannovation.Attempt++;
                                    logger.LogWarning(sqlexception, "Transient error, properly due to sql server not ready yet. We are backing off and trying again");
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create db");

                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });

                                    throw;
                                }

                            }

                            if (!modelResource.TryGetLastAnnotation(out EAVFWMigrationdAnnotation migrationannotation))
                            {
                                modelResource.Annotations.Add(migrationannotation = new EAVFWMigrationdAnnotation());
                            }

                            if (!migrationannotation.Success)
                            {

                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                  state => state with { State = new ResourceStateSnapshot("Creating Migrations", KnownResourceStateStyles.Info) });


                                try
                                {

                                    var connectionString = await targetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);

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
                                                    logger.LogInformation("Executing Migration SQL:\n{mig}", cmd.CommandText);
                                                    var r = await cmd.ExecuteNonQueryAsync();
                                                    Console.WriteLine("Rows changed: " + r);
                                                }
                                            }






                                        }

                                    }

                                   
                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                         state => state with { State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success) });
                                    migrationannotation.Success = true;

                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create migrations");

                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });

                                 
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (i == retryCount - 1)
                            {
                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                   state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });
                            }
                            else
                            {
                                await Task.Delay(delay);
                                delay *= 2; // Exponential backoff
                            }
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

}