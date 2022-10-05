using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;

namespace EAVFramework.UnitTest.ManifestTests
{
    public class BaseManifestTests
    {

        protected void AppendAttribute(JToken manifestC, string entityKey, string attributeKey, object attribute)
        {
            manifestC["entities"][entityKey]["attributes"][attributeKey] = JToken.FromObject(attribute);
        }

        protected object CreateCustomEntity(string name, string pluralName)
        {
            return new
            {
                schemaName = name,
                logicalName = name.Replace(" ", "").ToLower(),
                pluralName = pluralName,
                collectionSchemaName = pluralName.Replace(" ", ""),
                attributes = new
                {
                    id = new
                    {
                        isPrimaryKey = true,
                        schemaName = "Id",
                        logicalName = "id",
                        type = "guid"
                    },
                    name = new
                    {
                        schemaName = "Name",
                        logicalName = "name",
                        type = "string",
                        isPrimaryField = true
                    },

                }
            };
        }

        protected string RunDBWithSchema(string schema, params JToken[] manifests)
        {
            var configuration = new ConfigurationBuilder()
           .AddEnvironmentVariables()
           .Build();


            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IMigrationManager, MigrationManager>();

            services.AddOptions<DynamicContextOptions>().Configure((o) =>
            {
                o.Manifests = manifests;
                o.PublisherPrefix = "tests";
                o.EnableDynamicMigrations = true;
                o.Namespace = "DummyNamespace";

            });
            //services.AddEntityFrameworkSqlServer();
            services.AddDbContext<DynamicContext>((sp, optionsBuilder) =>
            {

                object p = optionsBuilder.UseSqlServer("dummy", x => x.MigrationsHistoryTable("__MigrationsHistory", schema));
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();

                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();


            });

            var sp = services.BuildServiceProvider();
            var ctx = sp.GetRequiredService<DynamicContext>();


            var migrator = ctx.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            //migrator.Migrate("0");
            //migrator.Migrate();
            return sql;

        }
    }
}
