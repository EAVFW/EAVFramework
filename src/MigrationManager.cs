using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
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
         IEdmModel Model { get; }
        Dictionary<string, Type> EntityDTOs { get; }
        Dictionary<string, Type> EntityDTOConfigurations { get; }
        public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options);

    } 
  

    public class MigrationManager: IMigrationManager
    {
        public  IEdmModel Model { get; set; }
        public MigrationManager(ILogger<MigrationManager> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public Dictionary<string, Type> EntityDTOs { get; } = new Dictionary<string,Type>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Type> EntityDTOConfigurations { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        public MethodInfo EntityTypeBuilderHasKey { get; private set; }
        public ConstructorInfo JsonPropertyAttributeCtor { get; private set; }

        //   public Assembly Assembly { get; set; }
        private ConcurrentDictionary<string, Migration> _migrations = new ConcurrentDictionary<string, Migration>();
        private readonly ILogger<MigrationManager> logger;

        public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options)
        {


            AppDomain myDomain = AppDomain.CurrentDomain;
            AssemblyName myAsmName = new AssemblyName(options.Namespace);

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
              AssemblyBuilderAccess.RunAndCollect);
           


            ModuleBuilder myModule =
              assemblyBuilder.DefineDynamicModule(options.Namespace + ".dll");


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
                     MigrationBuilderCreateIndex = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex),new Type[] {typeof(string),typeof(string),typeof(string[]),typeof(string),typeof(bool),typeof(string) }),
                     MigrationBuilderDropIndex = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)),

                     ColumnsBuilderType = typeof(ColumnsBuilder),
                     CreateTableBuilderType = typeof(CreateTableBuilder<>),
                     CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                     CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),

                     EntityTypeBuilderType = typeof(EntityTypeBuilder),
                     EntityTypeBuilderPropertyMethod = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), 0, new[] { typeof(string) }),
                     EntityTypeBuilderToTable = typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }),
                     EntityTypeBuilderHasKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[]) }),
                     EntityTypeBuilderHasAlternateKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasAlternateKey), 0, new[] { typeof(string[]) }),

                     ForeignKeyAttributeCtor = typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),
                     InverseAttributeCtor = typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) }),

                     EntityDTOs = EntityDTOs,
                     EntityDTOConfigurations=EntityDTOConfigurations,

                     OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),
                     ColumnsBuilderColumnMethod = typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance),
                     LambdaBase = typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null),

                     // EntityBaseClass = options.DTOBaseClass ?? typeof(DynamicEntity),
                     // BaseClassProperties = (options.DTOBaseClass ?? typeof(DynamicEntity)).GetProperties().Select(p=>p.Name).ToList(),
                     DTOBaseClasses = options.DTOBaseClasses ?? Array.Empty<Type>(),


                     EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                     EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),


                     JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                     JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),

                     DynamicTableType = typeof(IDynamicTable),
                     DynamicTableArrayType = typeof(IDynamicTable[]),

                     ReferentialActionType = typeof(ReferentialAction),
                     ReferentialActionNoAction = (int)ReferentialAction.NoAction,

                     DynamicMigrationType = typeof(DynamicMigration),
                     MigrationAttributeCtor = typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }),
                     OnDTOTypeGeneration=this.OnDTOTypeGeneration
                     

                 });

                 var migrationType = generator.CreateDynamicMigration(manifest);
                 var tables = generator.GetTables(manifest, myModule);
                // var tables = manifest.SelectToken("$.entities").OfType<JProperty>().Select(entity => generator.BuildEntityDefinition(myModule, manifest, entity)).ToArray().Select(entity => Activator.CreateInstance(entity.CreateTypeInfo()) as IDynamicTable).ToArray();

                 return Activator.CreateInstance(migrationType, manifest, tables) as Migration;
             });

            if (Model == null)
            {

                var builder = new ODataConventionModelBuilder();
                var v = new ODataModelBuilder();
                builder.EnableLowerCamelCase(NameResolverOptions.ProcessDataMemberAttributePropertyNames);
                
                //   builder.EntitySet<Movie>("Movies");
                //   builder.EntitySet<Review>("Reviews");

                foreach (var entity in EntityDTOs)
                {
                  //  logger.LogWarning("Creating Model for {entity}", entity.Key);
                    var config = builder.AddEntityType(entity.Value);

                    if (entity.Key.ToLower() == "identity")
                    {

                    }

                    //foreach(var nav in entity.Value.dto.GetProperties().Where(p => p.GetCustomAttribute<ForeignKeyAttribute>() != null))
                    //{
                    //    config.AddNavigationProperty(nav, Microsoft.OData.Edm.EdmMultiplicity.ZeroOrOne);
                    //    logger.LogWarning("Creating Nav for {entity}.{nav}", entity.Key,nav.Name);
                    //}

                    foreach (var nav in entity.Value.GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null))
                    {
                        if (nav.GetCustomAttribute<ForeignKeyAttribute>() is ForeignKeyAttribute navigation)
                        {
                            var prop = config.AddNavigationProperty(nav, Microsoft.OData.Edm.EdmMultiplicity.ZeroOrOne);
                            prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;
                          
                           // prop.DisableAutoExpandWhenSelectIsPresent = true;
                            logger.LogWarning("Creating Nav for {entity}.{nav} {prop}", entity.Key, nav.Name, prop.Name);
                        }
                        else
                        {
                            var prop = config.AddProperty(nav);
                            prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;
                            logger.LogWarning("Creating Prop for {entity}.{nav}", entity.Key, nav.Name);
                        }
                    }
                    //foreach(var col in entity.Value.GetProperties().Where(p => p.GetCustomAttribute<InversePropertyAttribute>() != null &&
                    //    p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)))
                    //{
                        
                    //    config.AddCollectionProperty(col);
                    //}
                    
                    foreach (var prop in config.Properties)
                    {
                        logger.LogWarning("Prop for {entity}.{Prop}", entity.Key, prop.Name);
                    }

                }
                Model = builder.GetEdmModel();
               
            }


            return new Dictionary<string, Migration>
            {
                [migration.GetId()] = migration
            };
        }



        public virtual void OnDTOTypeGeneration(JToken attributeDefinition, PropertyBuilder attProp)
        {

        }

    }
}
