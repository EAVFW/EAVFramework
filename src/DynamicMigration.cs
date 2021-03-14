using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace DotNetDevOps.Extensions.EAVFramwork
{

    public class DynamicContext : DbContext, IDynamicContext
    {

        private readonly string connectionString;
        private readonly string publisherPrefix;
        private readonly JToken data;

        public DynamicContext(DbContextOptions options, string connectionString, string publisherPrefix, JToken data)
            : base(options)


        {
            this.connectionString = connectionString;
            this.publisherPrefix = publisherPrefix;
            this.data = data;

        }

        public virtual IReadOnlyDictionary<string, Migration> GetMigrations()
        {
            var manager = new MigrationManager();
            var migration = manager.BuildMigration($"{publisherPrefix}_Initial", data);

            return new Dictionary<string, Migration>
            {
                [migration.GetId()] = migration
            };
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
