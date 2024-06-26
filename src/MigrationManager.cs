using EAVFramework.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using static EAVFramework.Shared.TypeHelper;
using EAVFramework.Shared.V2;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using EAVFW.Extensions.Manifest.SDK;
using System.Text.Json;

namespace EAVFramework
{

    public class SamplePocoImplemented
    {


        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
    public interface IMigrationManager
    {
        IEdmModel Model { get; }
        //   Dictionary<string, Type> EntityDTOs { get; }
        //  Dictionary<string, Type> EntityDTOConfigurations { get; }
        //public MigrationsInfo BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options);
        ModelDefinition ModelDefinition { get; }
        ModelDefinition EnusureBuilded(string name, JToken manifest, DynamicContextOptions options);
        ModelDefinition CreateModel(string migrationName, JToken manifest, DynamicContextOptions options);
        ModelDefinition CreateMigration(string migrationName, JToken afterManifest, JToken beforeManifest, DynamicContextOptions options);
    }
    public class ModelDefinition
    {
        public IEdmModel Model { get; set; }
        public Dictionary<string, Type> EntityDTOs { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Type> EntityDTOConfigurations { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public TypeInfo Type { get; set; }
        public Func<Migration> MigrationFactory { get; set; }
        public Dictionary<string, EntityDefinition> Entities { get; set; }
    }
    public class MigrationManagerOptions
    {
        public bool SkipValidateSchemaNameForRemoteTypes { get; set; } = true;
        public bool RequiredSupport { get; set; } = true;
        public string Schema { get; set; }
        public string Namespace { get; set; } = $"DynamicModule";
        public Assembly DTOAssembly { get; set; }
        public Type[] DTOBaseInterfaces { get; set; } = Array.Empty<Type>();
        public Type[] DTOBaseClasses { get; set; } = Array.Empty<Type>();

        public void CopyFrom(MigrationManagerOptions other)
        {
            this.SkipValidateSchemaNameForRemoteTypes = other.SkipValidateSchemaNameForRemoteTypes;
            this.RequiredSupport = other.RequiredSupport;
            this.Schema = other.Schema;
            this.Namespace = other.Namespace;
            this.DTOAssembly = other.DTOAssembly;
            this.DTOBaseClasses = other.DTOBaseClasses;
            this.DTOBaseInterfaces = other.DTOBaseInterfaces;
        }
    }
    public interface IDynamicCodeServiceFactory
    {
        DynamicCodeService Create(Action<CodeGenerationOptions> configure);
    }


    public class DynamicCodeServiceFactory : IDynamicCodeServiceFactory
    {
        public DynamicCodeService Create(Action<CodeGenerationOptions> configure)
        {
            return new DynamicCodeService(CreateOptions(configure));
        }

        public CodeGenerationOptions CreateOptions(Action<CodeGenerationOptions> configure)
        {
            var o = new CodeGenerationOptions
            {
                //  MigrationName="Initial",
                //  Schema = schema,
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

                /* .NET 8 added  descending*/
                MigrationBuilderCreateIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex),
                    new Type[] { typeof(string) /*name*/, typeof(string)/*table*/, typeof(string[]) /*columns*/, typeof(string)/*schema*/, typeof(bool) /*unique*/, typeof(string)/*filter*/ })
                ?? typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex),
                    new Type[] { typeof(string) /*name*/, typeof(string)/*table*/, typeof(string[]) /*columns*/, typeof(string)/*schema*/, typeof(bool) /*unique*/, typeof(string)/*filter*/, typeof(bool[]) /*descending*/ }), "MigrationBuilderCreateIndex"),
                
                
                MigrationBuilderDropIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)), "MigrationBuilderDropIndex"),
                MigrationsBuilderAddColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddColumn)), "MigrationsBuilderAddColumn"),
                MigrationsBuilderAddForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddForeignKey), new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(ReferentialAction), typeof(ReferentialAction) }), "MigrationsBuilderAddForeignKey"),
                MigrationsBuilderAlterColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AlterColumn)), "MigrationsBuilderAlterColumn"),
                MigrationsBuilderDropForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropForeignKey)), "MigrationsBuilderDropForeignKey"),

                ReferentialActionType = typeof(ReferentialAction),
                ReferentialActionNoAction = (int)ReferentialAction.NoAction,


                LambdaBase = Resolve(() => typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null), "LambdaBase"),

                GeoSpatialOptions = BuildNetTopologySuiteOptions()
            };
            configure(o);
            return o;
        }
        Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                   SingleOrDefault(assembly => assembly.GetName().Name == name);
        }
        public GeoSpatialOptions BuildNetTopologySuiteOptions()
        {
            var assembly = GetAssemblyByName("NetTopologySuite");
            if(assembly == null)
            {
                var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "NetTopologySuite.dll").FirstOrDefault();
                if (!string.IsNullOrEmpty(referencedPaths))
                {

                    assembly=AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPaths));
                }

                //
            }
            return new GeoSpatialOptions
            {
                PointGeomeryType = assembly?.GetType("NetTopologySuite.Geometries.Point")
            };
        }
    }
    public class MigrationManager : IMigrationManager
    {

        private readonly ILogger<MigrationManager> logger;
        private readonly IOptions<MigrationManagerOptions> options;
        private readonly DynamicCodeService dynamicCodeService;

        //   public  IEdmModel Model { get; set; }
        public MigrationManager(ILogger<MigrationManager> logger, IOptions<MigrationManagerOptions> options, IDynamicCodeServiceFactory dynamicCodeServiceFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.dynamicCodeService = dynamicCodeServiceFactory.Create(o =>
            {
                o.Schema = options.Value.Schema;
                o.DTOBaseClasses = options.Value.DTOBaseClasses;
                o.DTOBaseInterfaces = options.Value.DTOBaseInterfaces;
                o.RequiredSupport = options.Value.RequiredSupport;

            });
        }
        //public Dictionary<string, Type> EntityDTOs { get; } = new Dictionary<string,Type>(StringComparer.OrdinalIgnoreCase);
        //public Dictionary<string, Type> EntityDTOConfigurations { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        public MethodInfo EntityTypeBuilderHasKey { get; private set; }
        public ConstructorInfo JsonPropertyAttributeCtor { get; private set; }


        //   public Assembly Assembly { get; set; }
        private ConcurrentDictionary<string, Migration> _migrations = new ConcurrentDictionary<string, Migration>();
        //private ConcurrentDictionary<string, ModuleBuilder> _modules = new ConcurrentDictionary<string, ModuleBuilder>();




        public void Reset(DynamicContextOptions options)
        {
            //   Model = null;
            //   _modules.Remove(options.Namespace, out var _);
            dynamicCodeService.RemoveNamespace(this.options.Value.Namespace);
            _cache.Clear();
            //EntityDTOs.Clear();
            //EntityDTOConfigurations.Clear();
            Models.Clear();
        }

        public ConcurrentDictionary<string, ModelDefinition> Models { get; } = new ConcurrentDictionary<string, ModelDefinition>();
        public IEdmModel Model => ModelDefinition.Model;
        public ModelDefinition ModelDefinition => Models.FirstOrDefault(k => k.Key.Contains("latest")).Value;
        public ModelDefinition EnusureBuilded(string name, JToken manifest, DynamicContextOptions options)
        {
            return Models.GetOrAdd(name, _ =>
            {


                var m = CreateModel(name, manifest, options);

                var builder = new ODataConventionModelBuilder();

                // var v = new ODataModelBuilder();
                builder.EnableLowerCamelCase(NameResolverOptions.ProcessDataMemberAttributePropertyNames);

                //   builder.EntitySet<Movie>("Movies");
                //   builder.EntitySet<Review>("Reviews");

                foreach (var entity in m.EntityDTOs)
                {
                 
                    var config = builder.AddEntityType(entity.Value);
                    if (options.WithODATAEntitySet)
                    {
                        builder.AddEntitySet(entity.Key, config);
                    }

                 //   builder.Function(entity.Key + "Set").ReturnsCollectionFromEntitySet(entity.Value, entity.Key);

                    //foreach(var nav in entity.Value.dto.GetProperties().Where(p => p.GetCustomAttribute<ForeignKeyAttribute>() != null))
                    //{
                    //    config.AddNavigationProperty(nav, Microsoft.OData.Edm.EdmMultiplicity.ZeroOrOne);
                    //    logger.LogWarning("Creating Nav for {entity}.{nav}", entity.Key,nav.Name);
                    //}

                    foreach (var nav in entity.Value.GetProperties()
                        .Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null))
                    {
                        if (nav.GetCustomAttribute<InversePropertyAttribute>() is InversePropertyAttribute)
                        {
                            var prop = config.AddNavigationProperty(nav, Microsoft.OData.Edm.EdmMultiplicity.Many);
                            prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;

                        }
                        else if (nav.GetCustomAttribute<ForeignKeyAttribute>() is ForeignKeyAttribute navigation)
                        {
                            var prop = config.AddNavigationProperty(nav, Microsoft.OData.Edm.EdmMultiplicity.ZeroOrOne);
                            prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;

                            // prop.DisableAutoExpandWhenSelectIsPresent = true;
                            // logger.LogDebug("Creating Nav for {entity}.{nav} {prop}", entity.Key, nav.Name, prop.Name);
                        }
                        else if (nav.PropertyType.Assembly.FullName.Contains("NetTopologySuite"))
                        {
                            var prop = config.AddNavigationProperty(nav, Microsoft.OData.Edm.EdmMultiplicity.ZeroOrOne);
                            prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;
                        }
                        else
                        {
                            if ((Nullable.GetUnderlyingType(nav.PropertyType) ?? nav.PropertyType).IsEnum)
                            {
                                var prop = config.AddEnumProperty(nav);
                                prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;
                            }
                            else
                            {

                                var prop = config.AddProperty(nav);
                                prop.Name = nav.GetCustomAttribute<DataMemberAttribute>().Name;
                                //    logger.LogWarning("Creating Prop for {entity}.{nav}", entity.Key, nav.Name);
                            }
                        }
                    }
                    //foreach(var col in entity.Value.GetProperties().Where(p => p.GetCustomAttribute<InversePropertyAttribute>() != null &&
                    //    p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)))
                    //{

                    //    config.AddCollectionProperty(col);
                    //}



                }
              
               // builder.Function("Test").ReturnsCollectionFromEntitySet()
                m.Model = builder.GetEdmModel();
                return m;
                //}
            });


        }
        private ConcurrentDictionary<string, Lazy<ModelDefinition>> _cache = new ConcurrentDictionary<string, Lazy<ModelDefinition>>();

        /// <summary>
        /// Use this to loop over all manifests starting with beforeManifest being {}
        /// </summary>
        /// <param name="migrationName"></param>
        /// <param name="afterManifest"></param>
        /// <param name="beforeManifest"></param>
        /// <param name="options"></param>
        public ModelDefinition CreateMigration(string migrationName, JToken afterManifest, JToken beforeManifest, DynamicContextOptions options)
        {
            var afterEntities = GetEntities(afterManifest).Select(c => (c.Name, logicalName: c.Value.SelectToken("$.logicalName").ToString(), attributes: GetAttributes(c.Value))).ToArray();
            var beforeEntities = GetEntities(beforeManifest).Select(c => (c.Name, logicalName: c.Value.SelectToken("$.logicalName").ToString(), attributes: GetAttributes(c.Value))).ToArray();

            // var newEntities = afterEntities.Where(pair => !beforeEntities.Any(c => c.logicalName == pair.logicalName)).ToArray();
            //var updatedEntitiesByAddedAttributes = afterEntities.Where(pair => beforeEntities.FirstOrDefault(c => c.logicalName == pair.logicalName).attributes?.Any(attr=>pair.attributes)).ToArray();

            var manifest = afterManifest.DeepClone();

            //foreach (var entityDefinition in GetEntities(manifest).Where(entityDefinition => !newEntities.Any(entity=>entity.Name == entityDefinition.Name)))
            //{
            //    entityDefinition.Remove();
            //}

            return CreateModel(migrationName, manifest, options, true);



            static IEnumerable<JProperty> GetEntities(JToken manifest)
            {
                return (manifest.SelectToken("$.entities") ?? new JObject()).OfType<JProperty>().ToArray();
            }
            static IEnumerable<(string key, string logicalName)> GetAttributes(JToken entityDefinition)
            {
                return (entityDefinition.SelectToken("$.attributes") ?? new JObject()).OfType<JProperty>().Select(attr => (key: attr.Name, logicalName: attr.Value.SelectToken("$.logicalName").ToString())).ToArray();
            }
        }
        // public ConcurrentDictionary<string, TypeBuilder> EntityDTOsBuilders { get; internal set; } = new ConcurrentDictionary<string, TypeBuilder>();

        private ModelDefinition CreateModel(string migrationName, JToken manifest, DynamicContextOptions options, bool fromMigration)
        {
            return _cache.GetOrAdd(migrationName, (migrationName) => new Lazy<ModelDefinition>(() =>
            {

                try
                {
                    var m = new ModelDefinition()
                    {
                        Entities = new Dictionary<string, EntityDefinition>( System.Text.Json.JsonSerializer.Deserialize<ManifestDefinition>(manifest.ToString())
                        .Entities.ToDictionary(k=>k.Value.CollectionSchemaName, v=>v.Value),StringComparer.OrdinalIgnoreCase)
                    };
                    
                    //var asmb = dynamicCodeService.CreateAssemblyBuilder(options.Namespace);
                    var manfiestservice = new ManifestService(new ManifestServiceOptions
                    {
                        Namespace = this.options.Value.Namespace,
                        MigrationName = migrationName,
                        GenerateDTO = fromMigration ? false : true,
                        PartOfMigration = fromMigration,
                        EntityDTOConfigurations = m.EntityDTOConfigurations,
                        EntityDTOs = m.EntityDTOs,
                        DTOAssembly = this.options.Value.DTOAssembly,
                        SkipValidateSchemaNameForRemoteTypes = this.options.Value.SkipValidateSchemaNameForRemoteTypes,

                    });


                    //var myModule = _modules.GetOrAdd(options.Namespace, (name) =>
                    //{
                    //    AppDomain myDomain = AppDomain.CurrentDomain;
                    //    AssemblyName myAsmName = new AssemblyName(options.Namespace);

                    //    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
                    //      AssemblyBuilderAccess.RunAndCollect);



                    //    ModuleBuilder myModule =
                    //      assemblyBuilder.DefineDynamicModule(options.Namespace + ".dll");
                    //    return myModule;
                    //});


                    //var generator = new CodeGenerator(new CodeGeneratorOptions
                    //{
                    //    DTOAssembly = options.DTOAssembly,

                    //    GenerateDTO = fromMigration ? false : true,
                    //    PartOfMigration = fromMigration,
                    //    SkipValidateSchemaNameForRemoteTypes= this.options.Value.SkipValidateSchemaNameForRemoteTypes,
                    //    UseOnlyExpliciteExternalDTOClases=options.UseOnlyExpliciteExternalDTOClases,
                    //    //   EntityDTOsBuilders = EntityDTOsBuilders,

                    //    myModule = myModule,
                    //    Namespace = options.Namespace,
                    //    Schema = options.PublisherPrefix,
                    //    migrationName = migrationName,

                    //    MigrationBuilderDropTable = Resolve(()=> typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)), "MigrationBuilderDropTable"),
                    //    MigrationBuilderCreateTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)), "MigrationBuilderCreateTable"),
                    //    MigrationBuilderSQL = Resolve(()=>typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.Sql)), "MigrationBuilderSQL"),
                    //    MigrationBuilderCreateIndex = Resolve(()=>typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex), new Type[] { typeof(string), typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string) }),"MigrationBuilderCreateIndex"),
                    //    MigrationBuilderDropIndex = Resolve(()=> typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)), "MigrationBuilderDropIndex"),
                    //    MigrationsBuilderAddColumn = Resolve(()=> typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddColumn)), "MigrationsBuilderAddColumn"),
                    //    MigrationsBuilderAddForeignKey =Resolve(()=> typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddForeignKey),new Type[] {typeof(string), typeof( string ), typeof( string) , typeof(string) , typeof(string), typeof( string), typeof(string),typeof( ReferentialAction) , typeof(ReferentialAction )}), "MigrationsBuilderAddForeignKey"),
                    //    MigrationsBuilderAlterColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AlterColumn)), "MigrationsBuilderAlterColumn"),
                    //    MigrationsBuilderDropForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropForeignKey)), "MigrationsBuilderDropForeignKey"),

                    //    ColumnsBuilderType = typeof(ColumnsBuilder),
                    //    CreateTableBuilderType = typeof(CreateTableBuilder<>),
                    //    CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                    //    CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),

                    //    EntityTypeBuilderType = typeof(EntityTypeBuilder),
                    //    EntityTypeBuilderPropertyMethod = Resolve(()=> typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), 0, new[] { typeof(string) }), "EntityTypeBuilderPropertyMethod"),
                    //    EntityTypeBuilderToTable = Resolve(()=> typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }), "EntityTypeBuilderToTable"),
                    //    EntityTypeBuilderHasKey = Resolve(()=>typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[]) }), "EntityTypeBuilderHasKey"),
                    //    EntityTypeBuilderHasAlternateKey = Resolve(()=> typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasAlternateKey), 0, new[] { typeof(string[]) }), "EntityTypeBuilderHasAlternateKey"),



                    //    ForeignKeyAttributeCtor = Resolve(()=> typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }), "ForeignKeyAttributeCtor"),
                    //    InverseAttributeCtor = Resolve(()=> typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) }), "InverseAttributeCtor"),

                    //    EntityDTOs = EntityDTOs,
                    //    EntityDTOConfigurations = EntityDTOConfigurations,

                    //    OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),
                    //    ColumnsBuilderColumnMethod = Resolve(()=> typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance), "ColumnsBuilderColumnMethod"),
                    //    LambdaBase =Resolve(()=> typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null), "LambdaBase"),

                    //    // EntityBaseClass = options.DTOBaseClass ?? typeof(DynamicEntity),
                    //    // BaseClassProperties = (options.DTOBaseClass ?? typeof(DynamicEntity)).GetProperties().Select(p=>p.Name).ToList(),
                    //    DTOBaseClasses = options.DTOBaseClasses ?? Array.Empty<Type>(),
                    //    DTOBaseInterfaces = options.DTOBaseInterfaces ?? Array.Empty<Type>(),

                    //    EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                    //    EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),


                    //    JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                    //    JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),

                    //    DynamicTableType = typeof(IDynamicTable),
                    //    DynamicTableArrayType = typeof(IDynamicTable[]),

                    //    ReferentialActionType = typeof(ReferentialAction),
                    //    ReferentialActionNoAction = (int)ReferentialAction.NoAction,

                    //    DynamicMigrationType = typeof(DynamicMigration),
                    //    MigrationAttributeCtor = Resolve(()=> typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }), "MigrationAttributeCtor"),
                    //    OnDTOTypeGeneration = this.OnDTOTypeGeneration,

                    //    IsRequiredMethod = Resolve(()=> typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                    //           .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRequired)), "IsRequiredMethod"),
                    //    IsRowVersionMethod = Resolve(()=>typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                    //           .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRowVersion)), "IsRowVersionMethod"),
                    //    ValueGeneratedOnUpdate = Resolve(()=>typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                    //           .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.ValueGeneratedNever)), "ValueGeneratedOnUpdate"),
                    //    HasConversionMethod = Resolve(()=> typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                    //           .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasConversion),1, new Type[] { }), "HasConversionMethod"),
                    //    HasPrecisionMethod =Resolve(()=> typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                    //           .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasPrecision), new Type[] { typeof(int), typeof(int) }), "HasPrecisionMethod"),



                    // RequiredSupport = this.options.Value.RequiredSupport
                    //});




                    //var migrationType = generator.CreateDynamicMigration(manifest);
                    // var tables = generator.GetTables(manifest, myModule);

                    var (migrationType, tables) = manfiestservice.BuildDynamicModel(dynamicCodeService, manifest);

                    //   return (migrationType.GetTypeInfo(), () => Activator.CreateInstance(migrationType, manifest, tables) as Migration);
                    m.Type = migrationType.GetTypeInfo();
                    m.MigrationFactory = () => Activator.CreateInstance(migrationType, manifest, tables) as Migration;
                    return m;
                }
                catch (Exception ex)
                {
                    dynamicCodeService.RemoveNamespace(this.options.Value.Namespace);
                    //  _modules.Remove(options.Namespace, out var _);
                    throw;
                }
            })).Value;
        }



        public ModelDefinition CreateModel(string migrationName, JToken manifest, DynamicContextOptions options)
        {
            return this.CreateModel(migrationName, manifest, options, false);
        }
        //public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options)
        //{




        //    var migration= _migrations.GetOrAdd(migrationName, (migrationName) =>
        //     {

        //         // var tables = manifest.SelectToken("$.entities").OfType<JProperty>().Select(entity => generator.BuildEntityDefinition(myModule, manifest, entity)).ToArray().Select(entity => Activator.CreateInstance(entity.CreateTypeInfo()) as IDynamicTable).ToArray();
        //         var (migrationType, tables) = createModel(migrationName, manifest, options);
        //         return Activator.CreateInstance(migrationType, manifest, tables) as Migration;
        //     });


        ////    return new MigrationsInfo
        ////    {
        ////         Types = new Dictionary<string, Migration>
        ////         {
        ////             [migration.GetId()] = migration
        ////         };
        ////}
        //    return new Dictionary<string, Migration>
        //    {
        //        [migration.GetId()] = migration
        //    };
        //}



        public virtual void OnDTOTypeGeneration(JToken attributeDefinition, PropertyBuilder attProp)
        {

        }

    }
}
