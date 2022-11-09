using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;

namespace EAVFramework
{

    //https://stackoverflow.com/questions/27776761/ef-code-first-generic-entity-entitytypeconfiguration
    //https://stackoverflow.com/questions/26957519/ef-core-mapping-entitytypeconfiguration
    //https://github.com/dotnet/efcore/issues/21066
    //https://entityframeworkcore.com/knowledge-base/48060316/ef-core-2-0-dynamic-dbset

    //https://fahrigoktuna.medium.com/dynamic-unknown-types-for-database-operations-with-ef-core-1575302d1106





    [Serializable]
    public class DynamicEntity
    {
       
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
           
            //migrationBuilder.CreateTable(
            //              name: "test",
            //              columns: (c) => new Test(),
            //              schema: "hello",
            //              constraints: null,
            //              comment: "generated");

            foreach (var dynamicEntity in tables)
            {

               dynamicEntity.Up(migrationBuilder);

               // var dynamicTable  = new MigrationManager(null,null).buildColumns(entity);
                //migrationBuilder.CreateTable(
                //              name: "test",
                //              columns: columns => dynamicEntity.Columns(columns),
                //              schema: dynamicEntity.Schema,
                //              constraints: dynamicEntity.Constraints,
                //              comment: "generated");

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
