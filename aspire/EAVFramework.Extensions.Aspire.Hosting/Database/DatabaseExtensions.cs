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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Provisioning.Expressions;
using Azure.Storage.Sas;
using Microsoft.Data.SqlClient;

namespace EAVFramework.Extensions.Aspire.Hosting.Database
{
    public static class DatabaseExtensions
    {
        async static Task<string> CreateStoredAccessPolicyAsync(string containerName,string policyName, string connectionString)
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
           

            builder.ApplicationBuilder.Services.TryAddLifecycleHook((sp)=> new test(target.Resource.Parent));

            builder.WithCommand("BACKUP","Backup Database", async (context) =>
            {

                var connectionString = target.Resource.Parent.Outputs["blobStorageAccountKey"]?.ToString();

                var policy = await CreateStoredAccessPolicyAsync("backups","eav-backup", connectionString);

                var sqlConn = await builder.Resource.Parent.GetConnectionStringAsync();

                using var conn = new SqlConnection(sqlConn);

                await conn.OpenAsync();

                SqlCommand cmd = conn.CreateCommand();

               
              // var sql1 = builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
                var a = await target.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);

                a = a.TrimEnd('/');


                cmd.CommandText= $"""
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
                  Success=true,
                };
            });

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

        public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
             _parent.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new AzureStorageConnectionStringResolver());

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
            if(infrastructure is AzureResourceInfrastructure azure && azure.AspireResource is AzureStorageResource storage)
            {
                var storageResource = infrastructure.GetProvisionableResources().OfType<Azure.Provisioning.Storage.StorageAccount>().FirstOrDefault();
                storageResource.AllowSharedKeyAccess = true;
               
                    infrastructure.Add(new ProvisioningOutput("blobStorageAccountKey",
                        typeof(string))
                    { Value = BicepFunction.Interpolate($"DefaultEndpointsProtocol=https;AccountName={storageResource.Name};AccountKey={Value(storageResource.GetKeys()[0])};EndpointSuffix=core.windows.net"  ) });
            }
           

            base.ResolveInfrastructure(infrastructure, options);
        }
    }

}
