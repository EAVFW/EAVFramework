using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace DotNetDevOps.Extensions.EAVFramwork
{

    public class DynamicContextOptions{
        public JToken[] Manifests { get; set; }
        public string PublisherPrefix { get; set; }
        public bool EnableDynamicMigrations { get; set; }
        

    }
    public class DynamicContext : DbContext, IDynamicContext
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;

        public DynamicContext(DbContextOptions options,IOptions<DynamicContextOptions> modelOptions)
            : base(options)


        {
            this.modelOptions = modelOptions;
        }

        public virtual IReadOnlyDictionary<string, Migration> GetMigrations()
        {
            var manager = new MigrationManager();
            var migration = manager.BuildMigration($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(),this.modelOptions.Value);

            return new Dictionary<string, Migration>
            {
                [migration.GetId()] = migration
            };
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (this.modelOptions.Value.EnableDynamicMigrations)
            {
                ConfigureMigrationAsesmbly(optionsBuilder);
            }
            base.OnConfiguring(optionsBuilder);
        }
        protected virtual void ConfigureMigrationAsesmbly(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
        }


    }

    public class DynamicMigration : Migration
    {
        private readonly JToken model;
        private readonly IDynamicTable[] tables;

        public DynamicMigration(JToken model, IDynamicTable[] tables)
        {
            this.model = model;
            this.tables = tables;
        }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //    var model = JToken.Parse("{}");

            foreach (var dynamicEntity in tables)
            {

                dynamicEntity.Up(migrationBuilder);
                ///ynamicTable dynamicEntity = new MigrationManager().buildColumns(entity);
                //  migrationBuilder.CreateTable(
                //                name: dynamicEntity.Name,
                //                columns: columns => dynamicEntity.Columns(columns),
                //                schema: dynamicEntity.Schema,
                //                constraints: dynamicEntity.Constraints,
                //                comment: "generated");

                // migrationBuilder.CreateTable(
                //name: "PKSTests",
                //columns: table => new
                //{
                //    Id = table.Column<Guid>(nullable: false),
                //    EntityId=table.Column<Guid>(nullable:false)
                //},
                //constraints: table =>
                //{
                //    table.PrimaryKey("PK_PKSTests", x => x.Id);
                //    table.ForeignKey("FK_TEST",x=>x.EntityId,"principalTable","principalColumn","principalSchema", onUpdate:null,onDelete: null)//
                //});

            }

            //migrationBuilder.AddForeignKey()
        }



        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var dynamicEntity in tables.Reverse())
            {
                dynamicEntity.Down(migrationBuilder);

                //migrationBuilder.DropTable(
                //    name: dynamicEntity.Name);
            }

        }
    }
}
