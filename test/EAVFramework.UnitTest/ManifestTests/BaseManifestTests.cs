using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using EAVFramework.Shared.V2;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Newtonsoft.Json;
using System;
using static EAVFramework.Shared.TypeHelper;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EAVFW.Extensions.Manifest.SDK;

namespace EAVFramework.UnitTest.ManifestTests
{
    public class BaseManifestTests
    {
        private static void NoOp(CodeGenerationOptions o) { }
        protected static DynamicCodeService CreateOptions(Action<CodeGenerationOptions> onconfig = null)
        {
            onconfig ??= NoOp;
            var o = new CodeGenerationOptions
            {
                //  MigrationName="Initial",

                JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),
                JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                InverseAttributeCtor = typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) }),
                ForeignKeyAttributeCtor = typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),

                EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),
                EntityTypeBuilderType = typeof(EntityTypeBuilder),
                EntityTypeBuilderToTable = Resolve(() => typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }), "EntityTypeBuilderToTable"),
                EntityTypeBuilderHasKey = Resolve(() => typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[]) }), "EntityTypeBuilderHasKey"),
                EntityTypeBuilderPropertyMethod = Resolve(() => typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), 0, new[] { typeof(string) }), "EntityTypeBuilderPropertyMethod"),

                IsRequiredMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                   .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRequired)), "IsRequiredMethod"),
                IsRowVersionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                     .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRowVersion)), "IsRowVersionMethod"),
                HasConversionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                              .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasConversion), 1, new Type[] { }), "HasConversionMethod"),
                HasPrecisionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                   .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasPrecision), new Type[] { typeof(int), typeof(int) }), "HasPrecisionMethod"),



                DynamicTableType = typeof(IDynamicTable),
                DynamicTableArrayType = typeof(IDynamicTable[]),


                ColumnsBuilderType = typeof(ColumnsBuilder),
                CreateTableBuilderType = typeof(CreateTableBuilder<>),
                CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),
                ColumnsBuilderColumnMethod = Resolve(() => typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance), "ColumnsBuilderColumnMethod"),
                OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),


                MigrationBuilderDropTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)), "MigrationBuilderDropTable"),
                MigrationBuilderCreateTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)), "MigrationBuilderCreateTable"),
                MigrationBuilderSQL = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.Sql)), "MigrationBuilderSQL"),
                MigrationBuilderCreateIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex), new Type[] { typeof(string), typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string) }), "MigrationBuilderCreateIndex"),
                MigrationBuilderDropIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)), "MigrationBuilderDropIndex"),
                MigrationsBuilderAddColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddColumn)), "MigrationsBuilderAddColumn"),
                MigrationsBuilderAddForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddForeignKey), new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(ReferentialAction), typeof(ReferentialAction) }), "MigrationsBuilderAddForeignKey"),
                MigrationsBuilderAlterColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AlterColumn)), "MigrationsBuilderAlterColumn"),
                MigrationsBuilderDropForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropForeignKey)), "MigrationsBuilderDropForeignKey"),

                ReferentialActionType = typeof(ReferentialAction),
                ReferentialActionNoAction = (int)ReferentialAction.NoAction,


                LambdaBase = Resolve(() => typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null), "LambdaBase"),

            };
            onconfig(o);
            return new DynamicCodeService(o);
        }
    

    private static async Task ExecuteCommand(SqlConnection connection, string cmd)
        {
            try
            {


                var command = new SqlCommand(cmd, connection);


                SqlDataReader reader = await command.ExecuteReaderAsync();
                try
                {
                    while (reader.Read())
                    {


                    }
                }
                finally
                {
                    // Always call Close when done reading.
                    reader.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        public async Task<(IServiceProvider, Guid, ClaimsPrincipal)> Setup(Func<IServiceProvider,JToken> manifest)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddInMemoryCollection(new Dictionary<string, string>
                {

                    ["ConnectionStrings:ApplicationDB"] = "Server=127.0.0.1; Initial Catalog=DynManifest; User ID=sa; Password=Bigs3cRet; TrustServerCertificate=True",
                    ["ConnectionStrings:ApplicationDBMaster"] = "Server=127.0.0.1;  User ID=sa; Password=Bigs3cRet; TrustServerCertificate=True",


                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();


            services.AddCodeServices();
            services.AddEAVFramework<DynamicContext>();




            services.AddOptions<DynamicContextOptions>().Configure<IServiceProvider>((o,sp) =>
            {
                o.Manifests = new[]
                {
                  manifest(sp)
                };
                o.Schema = "dbo";

                o.EnableDynamicMigrations = true;
                o.Namespace = "DummyNamespace";
                //o.DTOBaseClasses = new[] { typeof(BaseOwnerEntity<Model.Identity>), typeof(BaseIdEntity<Model.Identity>) };
                o.DTOAssembly = typeof(UnitTest1).Assembly;

            });

            services.AddDbContext<DynamicContext>((sp, optionsBuilder) =>
            {

                optionsBuilder.UseSqlServer("Name=ApplicationDB", x => x.MigrationsHistoryTable("__MigrationsHistory", "dbo"));
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();

                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
                optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicContextModelCacheKeyFactory>();


            });

            var rootServiceProvider = services.BuildServiceProvider();

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {


                using (SqlConnection connection =
                    new SqlConnection(configuration.GetConnectionString("ApplicationDBMaster")))
                {

                    connection.Open();

                    await ExecuteCommand(connection, "DROP DATABASE [DynManifest]");

                    await ExecuteCommand(connection, "CREATE DATABASE [DynManifest];ALTER DATABASE [DynManifest] SET RECOVERY SIMPLE;");

                }



            }

            var principalId = Guid.Parse("1b714972-8d0a-4feb-b166-08d93c6ae328");
            var prinpal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub", principalId.ToString())
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme));




            return (rootServiceProvider, principalId, prinpal);
        }

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
        protected string RunDBWithSchema(string schema, Func<IServiceProvider, Task<JToken[]>> manifestProvider)
        {
            var configuration = new ConfigurationBuilder()
              .AddEnvironmentVariables()
              .Build();


            var services = new ServiceCollection();
            services.AddLogging();        
            services.AddCodeServices();

            services.AddManifestSDK<SQLClientParameterGenerator>();
           
            services.AddOptions<DynamicContextOptions>().Configure<IServiceProvider>((o,sp) =>
            {
                o.Manifests = manifestProvider(sp).Result;
                o.Schema = "tests";
                o.EnableDynamicMigrations = true;
                o.Namespace = "DummyNamespace";

                o.DTOBaseInterfaces = new[] { typeof(IAgreement<,>), typeof(IPaymentProvider<>), typeof(IPaymentProviderType) }; 

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
         
        protected string RunDBWithSchema(string schema, params JToken[] manifests)
        {
            return RunDBWithSchema(schema, (sp) => Task.FromResult( manifests));

        }
    }
}
