using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        //[TestMethod]
        //public async Task TestMethod1()
        //{

        //    var configuration = new ConfigurationBuilder()
        //    .AddEnvironmentVariables()
        //    .AddUserSecrets(this.GetType().Assembly)
        //    .Build();

        //    var model = JToken.FromObject(new
        //    {

        //        entities = new
        //        {
        //            Entity = new
        //            {
        //                schama = "test001",
        //                pluralName ="CustomEntities",
        //                logicalName="entity",
        //                attributes = new
        //                {
        //                    Id = new
        //                    {
        //                        logicalName="id",
        //                        type = new { type = "Guid", nullable = false },
        //                        isPrimaryKey = true,
        //                    },
        //                    Name = new
        //                    {
        //                        logicalName = "name",
        //                        type = new { type = "string", maxLength = 255, nullable = false },
        //                        isPrimaryKey = false,
        //                    }
        //                }
        //            }
        //        }
        //    });


        //    var schema = "dbo";

        //             var optionsBuilder = new DbContextOptionsBuilder<DynamicContext>();
        //  //  optionsBuilder.UseInMemoryDatabase("test");
        //    optionsBuilder.UseSqlServer(configuration.GetValue<string>("ConnectionString"), x => x.MigrationsHistoryTable("__MigrationsHistory", schema));
        //    optionsBuilder.EnableSensitiveDataLogging();
        //    optionsBuilder.EnableDetailedErrors();

        //    var ctx = new DynamicContext(optionsBuilder.Options, Options.Create(
        //         new DotNetDevOps.Extensions.EAVFramework.DynamicContextOptions
        //        {
        //            Manifests = new[] { model },
        //            PublisherPrefix = "test001", 
        //             EnableDynamicMigrations=true,
        //             Namespace="HelloWorld"
        //        }), new MigrationManager());

        //   var test =  ctx.GetMigrations();

        //    var migrator = ctx.Database.GetInfrastructure().GetRequiredService<IMigrator>();
           
        //    await migrator.MigrateAsync("0"); //Clean up
        //    await migrator.MigrateAsync(); //Move to latest migration


        //    ctx.Add("CustomEntities", JToken.FromObject(new { id=Guid.NewGuid(), name = "a" }));
        //    ctx.Add("CustomEntities", JToken.FromObject(new { id = Guid.NewGuid(), name = "ab" }));
        //    ctx.Add("CustomEntities", JToken.FromObject(new { id = Guid.NewGuid(), name = "a" }));
        //    ctx.Add("CustomEntities", JToken.FromObject(new { id = Guid.NewGuid(), name = "ab" }));
        //    await ctx.SaveChangesAsync();


        //    var set = ctx.Set("CustomEntities", "name eq 'a'");

        //    var result = await set.ToListAsync();


        //    var set1 = ctx.Set("CustomEntities", "name eq 'ab'");

        //    var result1 = await set1.ToListAsync();

        //    var set2 = ctx.Set("CustomEntities", "startswith(name,'a')");

        //    var result2 = await set2.ToListAsync();

        //    await migrator.MigrateAsync("0"); //Clean up


        //}
    }
}
