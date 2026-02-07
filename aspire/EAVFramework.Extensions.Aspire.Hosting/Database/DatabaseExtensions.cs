using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;
using Azure;
using Azure.Provisioning.Primitives;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Provisioning.Expressions;
using Azure.Storage.Sas;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.ApplicationModel.CommandResults;

#pragma warning disable ASPIREINTERACTION001 // Interaction service is in preview

namespace EAVFramework.Extensions.Aspire.Hosting.Database
{
    public static class DatabaseExtensions
    {
        async static Task<string> CreateStoredAccessPolicyAsync(string containerName, string policyName, string connectionString)
        {

            // Use the connection string to authorize the operation to create the access policy.
            // Azure AD does not support the Set Container ACL operation that creates the policy.
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

            try
            {
                await containerClient.CreateIfNotExistsAsync();

                // Create one or more stored access policies.
                List<BlobSignedIdentifier> signedIdentifiers = new List<BlobSignedIdentifier>
        {
            new BlobSignedIdentifier
            {
                Id = policyName,
                AccessPolicy = new BlobAccessPolicy
                {
                    StartsOn = DateTimeOffset.UtcNow.AddHours(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                    Permissions = "rw"
                }
            }
        };
                // Set the container's access policy.
                await containerClient.SetAccessPolicyAsync(permissions: signedIdentifiers);

                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c",
                    Identifier = policyName
                };



                Uri sasURI = containerClient.GenerateSasUri(sasBuilder);
                return sasURI.Query.ToString().Substring(1);
            }


            catch (RequestFailedException e)
            {
                Console.WriteLine(e.ErrorCode);
                Console.WriteLine(e.Message);
                return null;
            }
            finally
            {
                //  await containerClient.DeleteAsync();
            }
        }
        public static IResourceBuilder<SqlServerDatabaseResource> BackupTo(this IResourceBuilder<SqlServerDatabaseResource> builder, IResourceBuilder<AzureBlobStorageResource> target)
        {


            builder.ApplicationBuilder.Services.TryAddLifecycleHook((sp) => new test(target.Resource.Parent));

            builder.WithCommand("BACKUP", "Backup Database", async (context) =>
            {

                var connectionString = target.Resource.Parent.Outputs["blobStorageAccountKey"]?.ToString();

                var policy = await CreateStoredAccessPolicyAsync("backups", "eav-backup", connectionString);

                var sqlConn = await builder.Resource.Parent.GetConnectionStringAsync();

                using var conn = new SqlConnection(sqlConn);

                await conn.OpenAsync();

                SqlCommand cmd = conn.CreateCommand();


                // var sql1 = builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
                var a = await target.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);

                a = a.TrimEnd('/');


                cmd.CommandText = $"""
                  if exists (select * from sys.credentials where name = '{a}/backups')
                    drop credential [{a}/backups]
                  
                  CREATE CREDENTIAL [{a}/backups] WITH IDENTITY='Shared Access Signature', SECRET='{policy}'

                  BACKUP DATABASE MC TO URL = '{a}/backups/{target.Resource.Name}-{DateTimeOffset.UtcNow.ToString("yyyy-MMM-dd-HH-mm-ss-zzz")}.bak'
                	WITH FORMAT,  
                	COMPRESSION,
                	STATS=5,
                	BLOCKSIZE=65536,
                	MAXTRANSFERSIZE=4194304;

                """;

                await cmd.ExecuteNonQueryAsync();

                return new ExecuteCommandResult
                {
                    Success = true,
                };
            }, new CommandOptions { ConfirmationMessage = "Backup created", IsHighlighted = true });

            return builder;

        }

        /// <summary>
        /// Adds an interactive command to restore a BACPAC file to a SQL Server instance.
        /// The command will prompt for BACPAC file path, database name, and overwrite option.
        /// </summary>
        /// <param name="builder">The SQL Server resource builder</param>
        /// <param name="defaultDataDirectory">Optional default directory to search for BACPAC files. If null, uses "../data" relative to AppHost directory.</param>
        /// <param name="defaultDatabaseName">Default database name for restore. Defaults to "sql-db".</param>
        /// <param name="commandName">Custom command name. Defaults to "restore-bacpac".</param>
        /// <param name="displayName">Custom display name. Defaults to "Restore BACPAC File".</param>
        /// <param name="iconName">Custom icon name. Defaults to "DatabaseArrowDown".</param>
        /// <returns>The SQL Server resource builder for chaining</returns>
        public static IResourceBuilder<SqlServerServerResource> WithRestoreBacpacCommand(
            this IResourceBuilder<SqlServerServerResource> builder,
            string? defaultDataDirectory = null,
            string defaultDatabaseName = "sql-db",
            string commandName = "restore-bacpac",
            string displayName = "Restore BACPAC File",
            string iconName = "DatabaseArrowDown")
        {
            builder.WithCommand(
                name: commandName,
                displayName: displayName,
                executeCommand: async context =>
                {
                    var interactionService = context.ServiceProvider
                        .GetRequiredService<IInteractionService>();

                    // Auto-detect BACPAC files in data directory
                    var appHostDir = builder.ApplicationBuilder.AppHostDirectory;
                    var dataDir = string.IsNullOrEmpty(defaultDataDirectory)
                        ? Path.GetFullPath(Path.Combine(appHostDir, "..", "..", "data"))
                        : Path.GetFullPath(defaultDataDirectory);

                    string? defaultBacpacPath = null;

                    if (Directory.Exists(dataDir))
                    {
                        var bacpacFiles = Directory.GetFiles(dataDir, "*.bacpac");
                        if (bacpacFiles.Length > 0)
                        {
                            defaultBacpacPath = Path.GetFullPath(bacpacFiles[0]);
                        }
                    }

                    // Prompt for inputs
                    var inputs = new List<InteractionInput>
                    {
                        new()
                        {
                            Name = "FilePath",
                            Label = "BACPAC File Path",
                            InputType = InputType.Text,
                            Required = true,
                            Value = defaultBacpacPath,
                            Placeholder = "/path/to/backup.bacpac"
                        },
                        new()
                        {
                            Name = "DatabaseName",
                            Label = "Target Database Name",
                            InputType = InputType.Text,
                            Required = true,
                            Value = defaultDatabaseName,
                            Placeholder = defaultDatabaseName
                        },
                        new()
                        {
                            Name = "OverwriteExisting",
                            Label = "Overwrite if database exists?",
                            InputType = InputType.Boolean,
                            Required = false,
                            Value = "true"
                        }
                    };

                    var result = await interactionService.PromptInputsAsync(
                        title: "Restore BACPAC Database",
                        message: "Specify the BACPAC file location and target database:",
                        inputs: inputs);

                    if (result.Canceled)
                        return Canceled();

                    var filePath = result.Data[0].Value;
                    var databaseName = result.Data[1].Value;
                    var overwrite = bool.Parse(result.Data[2].Value ?? "false");

                    try
                    {
                        // Validate file exists
                        if (!File.Exists(filePath))
                        {
                            return Failure($"File not found: {filePath}");
                        }

                        // Get the SQL Server connection string with the actual allocated endpoint
                        var baseConnectionString = await builder.Resource.GetConnectionStringAsync(context.CancellationToken);

                        if (string.IsNullOrEmpty(baseConnectionString))
                        {
                            return Failure("Could not get SQL Server connection string. Ensure SQL Server is running.");
                        }

                        // Modify connection string to connect to 'master' database for management operations
                        var sqlBuilder = new SqlConnectionStringBuilder(baseConnectionString)
                        {
                            InitialCatalog = "master"
                        };
                        var connectionString = sqlBuilder.ConnectionString;

                        // Check if database exists and handle overwrite
                        if (overwrite)
                        {
                            using var checkConn = new SqlConnection(connectionString);
                            await checkConn.OpenAsync(context.CancellationToken);
                            using var cmd = checkConn.CreateCommand();
                            cmd.CommandText = $@"
                                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
                                BEGIN
                                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                    DROP DATABASE [{databaseName}];
                                END";
                            await cmd.ExecuteNonQueryAsync(context.CancellationToken);
                        }

                        // Use DacFx library to import the BACPAC
                        using var bacPackage = Microsoft.SqlServer.Dac.BacPackage.Load(filePath);
                        var dacServices = new Microsoft.SqlServer.Dac.DacServices(connectionString);

                        // Optional: Subscribe to progress events for better UX
                        dacServices.Message += (sender, e) =>
                            Console.WriteLine($"[BACPAC Import] {e.Message.MessageType}: {e.Message.Message}");

                        // Import with options
                        var importOptions = new Microsoft.SqlServer.Dac.DacImportOptions
                        {
                            CommandTimeout = 0 // Unlimited timeout for large databases
                        };

                        dacServices.ImportBacpac(bacPackage, databaseName, importOptions, context.CancellationToken);

                        return Success();
                    }
                    catch (Exception ex)
                    {
                        return Failure($"Error: {ex.Message}");
                    }
                },
                updateState: context =>
                    context.ResourceSnapshot.State?.Text == "Running"
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled,
                iconName: iconName,
                isHighlighted: true
            );

            return builder;
        }


    }

    public class test : IDistributedApplicationLifecycleHook
    {
        private AzureStorageResource _parent;

        public test(AzureStorageResource parent)
        {
            _parent = parent;
        }

        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            _parent.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new AzureStorageConnectionStringResolver());
            return Task.CompletedTask;
        }


    }
    internal class AzureStorageConnectionStringResolver : InfrastructureResolver
    {

        public static BicepValue<string> Value(BicepValue<object> value)
        {
            return new MemberExpression(value.Compile(), "value");


        }

        public override void ResolveInfrastructure(Azure.Provisioning.Infrastructure infrastructure, ProvisioningBuildOptions options)
        {
            if (infrastructure is AzureResourceInfrastructure azure && azure.AspireResource is AzureStorageResource storage)
            {
                var storageResource = infrastructure.GetProvisionableResources().OfType<Azure.Provisioning.Storage.StorageAccount>().FirstOrDefault();
                storageResource.AllowSharedKeyAccess = true;

                infrastructure.Add(new ProvisioningOutput("blobStorageAccountKey",
                    typeof(string))
                { Value = BicepFunction.Interpolate($"DefaultEndpointsProtocol=https;AccountName={storageResource.Name};AccountKey={Value(storageResource.GetKeys()[0])};EndpointSuffix=core.windows.net") });
            }


            base.ResolveInfrastructure(infrastructure, options);
        }
    }

}