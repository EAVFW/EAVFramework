using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.OData.ModelBuilder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using PropertyBuilder = System.Reflection.Emit.PropertyBuilder;

namespace DotNetDevOps.Extensions.EAVFramework
{

    public class SamplePocoImplemented
    {

         
        [DataMember(Name="name")]
        public string Name { get; set; }
    }
    public interface IMigrationManager
    {
        Dictionary<string, Type> EntityDTOs { get; }
        Dictionary<string, Type> EntityDTOConfigurations { get; }
        public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options);

    } 
    public class MigrationManager: IMigrationManager
    {
        public Dictionary<string, Type> EntityDTOs { get; } = new Dictionary<string,Type>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Type> EntityDTOConfigurations { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        public MethodInfo EntityTypeBuilderHasKey { get; private set; }
        public ConstructorInfo JsonPropertyAttributeCtor { get; private set; }

        //   public Assembly Assembly { get; set; }
        private ConcurrentDictionary<string, Migration> _migrations = new ConcurrentDictionary<string, Migration>();
        public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options)
        {


            AppDomain myDomain = AppDomain.CurrentDomain;
            AssemblyName myAsmName = new AssemblyName(options.Namespace);

            var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
              AssemblyBuilderAccess.RunAndCollect);
           


            ModuleBuilder myModule =
              builder.DefineDynamicModule(options.Namespace + ".dll");


            var migration= _migrations.GetOrAdd(migrationName, (migrationName) =>
             {
                 var generator = new CodeGenerator(new CodeGeneratorOptions
                 {
                     DTOAssembly = options.DTOAssembly,
                     
                     myModule = myModule,
                     Namespace = options.Namespace,
                     Schema = options.PublisherPrefix,
                     migrationName = migrationName,
                      
                     MigrationBuilderDropTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)),
                     MigrationBuilderCreateTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)),
                     ColumnsBuilderType = typeof(ColumnsBuilder),
                     CreateTableBuilderType = typeof(CreateTableBuilder<>),
                     CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                     CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),

                     EntityTypeBuilderType = typeof(EntityTypeBuilder),
                     EntityTypeBuilderPropertyMethod = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), 0, new[] { typeof(string) }),
                     EntityTypeBuilderToTable = typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }),
                     EntityTypeBuilderHasKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[]) }),

                     ForeignKeyAttributeCtor = typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),

                     EntityDTOs = EntityDTOs,
                     EntityDTOConfigurations=EntityDTOConfigurations,

                     OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),
                     ColumnsBuilderColumnMethod = typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance),
                     LambdaBase = typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null),

                     // EntityBaseClass = options.DTOBaseClass ?? typeof(DynamicEntity),
                     // BaseClassProperties = (options.DTOBaseClass ?? typeof(DynamicEntity)).GetProperties().Select(p=>p.Name).ToList(),
                     DTOBaseClasses = options.DTOBaseClasses,


                     EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                     EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),


                     JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                     JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),

                     DynamicTableType = typeof(IDynamicTable),
                     DynamicTableArrayType = typeof(IDynamicTable[]),

                     ReferentialActionType = typeof(ReferentialAction),
                     ReferentialActionNoAction = (int)ReferentialAction.NoAction,

                     DynamicMigrationType = typeof(DynamicMigration),
                     MigrationAttributeCtor = typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) })

                 });

                 var migrationType = generator.CreateDynamicMigration(manifest);
                 var tables = manifest.SelectToken("$.entities").OfType<JProperty>().Select(entity => Activator.CreateInstance(generator.BuildEntityDefinition(myModule, manifest, entity)) as IDynamicTable).ToArray();

                 return Activator.CreateInstance(migrationType, manifest, tables) as Migration;
             });

            return new Dictionary<string, Migration>
            {
                [migration.GetId()] = migration
            };
        }

      


    }
}
