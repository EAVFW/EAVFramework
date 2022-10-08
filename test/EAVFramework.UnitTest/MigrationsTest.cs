using EAVFramework.UnitTest.ManifestTests;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Options;

namespace EAVFramework.UnitTest
{
    [TestClass]
    public class MigrationsTest : BaseManifestTests
    {

        public async Task<(IServiceProvider, Guid, ClaimsPrincipal)> Setup()
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


            services.AddSingleton<IMigrationManager, MigrationManager>();
            services.AddEAVFramework<DynamicContext>();
                

            

            services.AddOptions<DynamicContextOptions>().Configure((o) =>
            {
                o.Manifests = new[]
                {
                   JToken.FromObject(new
                   {
                       version = "1.0.0",
                       entities = new
                       {
                          Car = CreateCustomEntity("Car", "Cars"),
                       }
                   })
                };
                o.PublisherPrefix = "dbo";

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

        [TestMethod]
        public async Task Test()
        {
            var (rootServiceProvider, principalId, prinpal) = await Setup();


            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();
                
                await ctx.MigrateAsync();
                 

            }


            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();
               
                var o = sp.GetService<IOptions<DynamicContextOptions>>();
                o.Value.Manifests = new[]
                {
                    JToken.FromObject(new
                   {
                       version = "1.0.1",
                       entities = new
                       {
                          Car = CreateCustomEntity("Car", "Cars"),
                          Truck = CreateCustomEntity("Truck", "Trucks"),
                       }
                   }),

                }.Concat(o.Value.Manifests).ToArray();

                ctx.ResetMigrationsContext();
               
               
                await ctx.MigrateAsync();


            }

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVFramework.Endpoints.EAVDBContext<DynamicContext>>();

                ctx.Add("Trucks", JObject.FromObject(new { name = "a" }));

                await ctx.SaveChangesAsync(prinpal);

            }

            }
        }
}
