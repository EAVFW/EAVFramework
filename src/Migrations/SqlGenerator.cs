using EAVFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Manifest.SDK.Migrations
{
    public class MigrationResult
    {
        public JToken Model { get; set; }
        public string SQL { get; set; }
        public string Permissions { get; set; }

    }
    public class SQLMigrationGenerator
    {
        private readonly IManifestPermissionGenerator manifestPermissionGenerator;

        public SQLMigrationGenerator(IManifestPermissionGenerator manifestPermissionGenerator)
        {

            this.manifestPermissionGenerator = manifestPermissionGenerator;
        }

        public async Task<MigrationResult> GenerateSQL(string projectPath, bool shouldGeneratePermissions, string systemEntity,
            Action<SqlServerDbContextOptionsBuilder> extend=null)
        {
            var schema = "$(DBSchema)";
            var model = JToken.Parse(File.ReadAllText(Path.Combine(projectPath, "obj", "manifest.g.json")));
            var models = Directory.Exists(Path.Combine(projectPath, "manifests")) ? Directory.EnumerateFiles(Path.Combine(projectPath, "manifests"))
                .Select(file => JToken.Parse(File.ReadAllText(file)))
                .OrderByDescending(k => Semver.SemVersion.Parse(k.SelectToken("$.version").ToString(), Semver.SemVersionStyles.Strict))
                .ToArray() : Array.Empty<JToken>();

            var optionsBuilder = new DbContextOptionsBuilder<DynamicContext>();
            //  optionsBuilder.UseInMemoryDatabase("test");
           optionsBuilder.UseSqlServer("dummy",   x =>
           {
               x.MigrationsHistoryTable("__MigrationsHistory", schema);
               extend?.Invoke(x);
           }
           );
         
            optionsBuilder.EnableSensitiveDataLogging();

            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();

            var ctx = new DynamicContext(optionsBuilder.Options, Microsoft.Extensions.Options.Options.Create(
                 new EAVFramework.DynamicContextOptions
                 {
                     Manifests = new[] { model }.Concat(models).ToArray(),
                     Schema = schema,
                     EnableDynamicMigrations = true,
                     Namespace = "EAVFW.Extensions.Manifest",
                     // DTOAssembly = typeof(ApplicationExtensions).Assembly,
                     // DTOBaseClasses = new[] { typeof(BaseOwnerEntity<Model.Identity>), typeof(BaseIdEntity<Model.Identity>), typeof(KeyValueEntity<Model.Identity>) }
                 }),
                 new MigrationManager(NullLogger<MigrationManager>.Instance, Microsoft.Extensions.Options.Options.Create(new MigrationManagerOptions()
                 {
                     SkipValidateSchemaNameForRemoteTypes = true,
                     Schema = schema,
                     Namespace = "EAVFW.Extensions.Manifest",
                 }), new DynamicCodeServiceFactory())
                 , NullLogger<DynamicContext>.Instance);



            var migrator = ctx.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

            var result = new MigrationResult { Model = model, SQL = sql }; ;

            if (shouldGeneratePermissions)
                result.Permissions = await InitializeSystemAdministrator(result.Model, systemEntity);

            return result;
        }
        public async Task<string> InitializeSystemAdministrator(JToken model, string systemUserEntity)
        {
            //TODO : Fix such this is only done if security package is installed.

            var sb = await manifestPermissionGenerator.CreateInitializationScript(model, systemUserEntity);

            return sb;

        }


    }
}
