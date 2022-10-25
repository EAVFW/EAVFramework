using EAVFramework.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Container;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EAVFramework
{

    public class QueryContext<TContext> where TContext :  DynamicContext
    {
        public DynamicContext Context { get; set; }
        public Type Type { get; set; }
        public HttpRequest Request { get; set; }

        public Dictionary<IQueryExtender<TContext>, bool> SkipQueryExtenders { get; set; } = new Dictionary<IQueryExtender<TContext>, bool>();
    }

    //public class QueryContext : QueryContext<DynamicContext>
    //{
        
    //}

    //public interface IQueryExtender
    //{
    //    IQueryable ApplyTo(IQueryable metadataQuerySet, QueryContext context);
    //} 
    public interface IQueryExtender<TContext>  where TContext : DynamicContext
    {
        IQueryable ApplyTo(IQueryable metadataQuerySet, QueryContext<TContext> context);
    }

    public static class TypeChanger
    {
        public static IQueryable<T> ChangeType<T>(IQueryable data)
        {
            return data.Cast<T>();// as IQueryable<T>;
        }

        public static IQueryable Cast(this IQueryable data, Type type)
        {
            return (IQueryable)typeof(TypeChanger).GetMethod(nameof(TypeChanger.ChangeType)).MakeGenericMethod(type).Invoke(null, new[] { data });
        }
    }

    public interface IODataConverter
    {
        object Convert(object data);

    }
    public interface IODataConverterFactory
    {
        IODataConverter CreateConverter(Type type);
    }
    public interface IODataRuntimeType
    {
        string GetDataType(object data);
    }
    public interface IODataRuntimeTypeFactory
    {
        IODataRuntimeType CreateTypeParser(Type type, object data);
    }
    public class SelectSomeOfT : IODataRuntimeType
    {
        private IODataRuntimeTypeFactory oDataRuntimeTypeFactory;

       
        private static PropertyInfo untypedInstance  =  typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
            .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper")?.GetProperty("UntypedInstance");
        private static PropertyInfo container = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
            .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper")?.GetProperty("Container");

        private static MethodInfo getElementType = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
           .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper")?.GetMethod("GetElementType", BindingFlags.NonPublic | BindingFlags.Instance);

        public SelectSomeOfT(IODataRuntimeTypeFactory oDataRuntimeTypeFactory)
        {
            this.oDataRuntimeTypeFactory=oDataRuntimeTypeFactory;
           
        }
        private ConcurrentDictionary<Type, PropertyInfo> _NamedPropertyBag = new ConcurrentDictionary<Type, PropertyInfo>();
        private ConcurrentDictionary<string, string> LogicalNameMapping = new ConcurrentDictionary<string, string>();

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public string GetDataType(object data)
        {
            var test = data as  IEdmObject;
            var modeltype = test.GetEdmType();
            var serializedType = LogicalNameMapping.GetOrAdd(modeltype.Definition.FullTypeName(), (typename) =>
            {
                return GetType(typename)?.GetCustomAttribute<EntityAttribute>()?.LogicalName??typename;

            });

            //var instance = untypedInstance?.GetValue(data);
            //if (instance== null)
            //{
            //    var aa = getElementType.Invoke(data,null);


            //    var c = container.GetValue(data);
            //    var namedProperty = _NamedPropertyBag.GetOrAdd(c.GetType(), (t) => t.GetProperty("Value"));
            //    var n = namedProperty.GetValue(c);
            //    instance=untypedInstance.GetValue(n);
            //}
            //var serializedType = instance?.GetType().GetCustomAttribute<EntityAttribute>()?.LogicalName;
            return serializedType;
        }
    }

    public class ConstantRuntimeType : IODataRuntimeType
    {
        private string logicalName;

        public ConstantRuntimeType(string logicalName)
        {
            this.logicalName=logicalName;
        }

        public string GetDataType(object data)
        {
            return logicalName;
        }
    }
    public class ODataRuntimeTypeFactory : IODataRuntimeTypeFactory
    {
        private static ConcurrentDictionary<Type, IODataRuntimeType> _typeParsers = new ConcurrentDictionary<Type, IODataRuntimeType>();
        private static Type selectexpandwrapper = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
            .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper`1");
       

        public IODataRuntimeType CreateTypeParser(Type type, object data)
        {
            return ODataRuntimeTypeFactory._typeParsers.GetOrAdd(type, type=>Factory(type,data));
           
        }

        private IODataRuntimeType Factory(Type type, object data)
        {
            //
            if (type.IsGenericType && 
                ODataRuntimeTypeFactory.selectexpandwrapper.MakeGenericType(type.GenericTypeArguments[0]).IsAssignableFrom(type) &&
                 
                type.GenericTypeArguments[0].GetCustomAttribute<EntityAttribute>() is EntityAttribute attr && !attr.IsBaseClass)
            {
                return new ConstantRuntimeType(attr.LogicalName);
            }

            return new SelectSomeOfT(this);

        }
    }
    internal class SelectCoverter : IODataConverter
    {
        private static IODataRuntimeTypeFactory typeParser = new ODataRuntimeTypeFactory();

        private Type type;
        private IODataConverterFactory odatatConverterFactory;
        private MethodInfo entityProperty;
      
        

        //  private PropertyInfo namedProperty;
        private Func<IEdmModel, IEdmStructuredType, IPropertyMapper> MapperProvider;
        //private readonly string serializedType;
        public SelectCoverter(Type type, IODataConverterFactory odatatConverterFactory)
        {
            this.type = type;
            this.odatatConverterFactory = odatatConverterFactory;
            this.entityProperty = type.GetMethod("ToDictionary", new[] { typeof(Func<IEdmModel, IEdmStructuredType, IPropertyMapper>) });
            var SelectExpandWrapperConverter = type.Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapperConverter");
            this.MapperProvider =(Func<IEdmModel, IEdmStructuredType, IPropertyMapper>) SelectExpandWrapperConverter.GetField("MapperProvider", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
          
            

           // this.namedProperty =type.Assembly.GetType("Microsoft.AspNetCore.OData.Query.Container.NamedProperty`1")?.GetProperty("Value");
           //serializedType = $"{type.GetGenericArguments().First().FullName}, {type.GetGenericArguments().First().Assembly.GetName().Name}";
        }

       

        //Microsoft.AspNetCore.OData.Query.Container.NamedProperty<T> //https://github.com/OData/AspNetCoreOData/blob/main/src/Microsoft.AspNetCore.OData/Query/Container/NamedPropertyOfT.cs



        public object Convert(object data)
        {
             
            //https://github.com/OData/AspNetCoreOData/blob/main/src/Microsoft.AspNetCore.OData/Query/Wrapper/SelectAllOfT.cs
            //Microsoft.AspNetCore.OData.Query.Wrapper.SelectAll<KFST.Vanddata.Model.Identity>
            
            var poco =(data as Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper).ToDictionary(MapperProvider);

            var typeParser = SelectCoverter.typeParser.CreateTypeParser(data.GetType(),data);
            // var poco = (IDictionary<string, object>)entityProperty.Invoke(data, new object[] { MapperProvider });
            poco["$type"] = typeParser.GetDataType(data);

            foreach (var kv in poco.ToArray())
            {
                if(kv.Value == null)
                {
                    poco.Remove(kv.Key);
                    continue;
                }

                var converter = odatatConverterFactory.CreateConverter(kv.Value.GetType());
                var value= converter.Convert(kv.Value);
                if (value == null)
                {
                    poco.Remove(kv.Key);
                }
                else
                {
                    poco[kv.Key] = value;
                }
            }

            return poco;
        }
    }
    internal class EnumerableConverter : IODataConverter
    {
        private Type type;
        private IODataConverterFactory odatatConverterFactory;

        public EnumerableConverter(Type type, IODataConverterFactory odatatConverterFactory)
        {
            this.type = type;
            this.odatatConverterFactory = odatatConverterFactory;
        }

        public object Convert(object data)
        {
            if (data is byte[])
                return data;

            var list = new List<object>();
            foreach (var i in data as IEnumerable)
            {
                var converter = odatatConverterFactory.CreateConverter(i.GetType());
                list.Add(converter.Convert(i));
            }
            return list;
        }
    }
    public class OdatatConverterFactory : IODataConverterFactory
    {
        private static ConcurrentDictionary<Type, IODataConverter> _converters = new ConcurrentDictionary<Type, IODataConverter>();

        public IODataConverter CreateConverter(Type type)
        {
            return _converters.GetOrAdd(type, type =>
             {
                 if (type.Name == "SelectAllAndExpand`1")
                 {
                     return new SelectAllAndExpandConverter(type, this);

                 }
                 else if (type.Name == "SelectSome`1" || type.Name == "SelectAll`1" || type.Name == "SelectSomeAndInheritance`1")
                 {
                     return new SelectCoverter(type, this);


                 }
                 else if (typeof(IEnumerable).IsAssignableFrom(type) && (type != typeof(string)))
                 {
                     return new EnumerableConverter(type, this);

                 }
                 else
                 {
                     return new PrimitivConverter();
                 }

             });

        }
    }
    internal class SelectAllAndExpandConverter : IODataConverter
    {
        private readonly Type type;
        private OdatatConverterFactory odatatConverterFactory;
        private PropertyInfo entityProperty;

        public SelectAllAndExpandConverter(Type type, OdatatConverterFactory odatatConverterFactory)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.odatatConverterFactory = odatatConverterFactory ?? throw new ArgumentNullException(nameof(odatatConverterFactory));
            this.entityProperty = type.GetProperty("Instance");
        }

        public object Convert(object data)
        {
            var value = entityProperty.GetValue(data);

            var converter = odatatConverterFactory.CreateConverter(value.GetType());

            return converter.Convert(value);
        }
    }

    internal class PrimitivConverter : IODataConverter
    {
        public object Convert(object data)
        {
            
            return data;
        }
    }

    public interface IFormContextFeature<TDynamicContext> where TDynamicContext : DynamicContext
    {
        public ValueTask<JToken> GetManifestAsync();
    }
    public class DefaultFormContextFeature<TDynamicContext> :IFormContextFeature<DynamicContext> where TDynamicContext : DynamicContext
    {
        private readonly IOptions<DynamicContextOptions> options;

        public DefaultFormContextFeature(IOptions<DynamicContextOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ValueTask<JToken> GetManifestAsync()
        {
            return new ValueTask<JToken>(options.Value.Manifests.First());
        }
    }

    public class UtcValueConverter : ValueConverter<DateTime, DateTime>
    {
        public static UtcValueConverter Instance = new UtcValueConverter();
        public UtcValueConverter()
            : base(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    public static class DynamicContextExtensions
    {
        private static OdatatConverterFactory _factory = new OdatatConverterFactory();

        private static object ToPoco(object item)
        {
            if (item == null)
                return null;

            var converter = _factory.CreateConverter(item.GetType());
            return converter.Convert(item);



        }


        public static async Task<PageResult<object>> ExecuteHttpRequest<TContext>(this TContext context, string entityCollectionSchemaName, HttpRequest request) where TContext:DynamicContext
        {
            var t1 = typeof(IQueryExtender<>).MakeGenericType(typeof(TContext));
            var t2 = typeof(IEnumerable<>).MakeGenericType(t1);
            var queryInspectors = (request.HttpContext.RequestServices.GetService(t2) as IEnumerable).Cast<IQueryExtender<TContext>>()
                .ToList();

            context.EnsureModelCreated();

            var type = context.Manager.ModelDefinition.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];

            var metadataQuerySet = context.Set(type);

            var queryContext = new QueryContext<TContext>
            {
                Type = type,
                Request = request,
                SkipQueryExtenders = queryInspectors.ToDictionary(x => x, v => false),
                Context = context
            };


            foreach (var queryInspector in queryInspectors.Where(q => !queryContext.SkipQueryExtenders[q]))
                metadataQuerySet = queryInspector.ApplyTo(metadataQuerySet, queryContext).Cast(type) ?? metadataQuerySet;




            if (request != null)
            {
                if (!request.Query.ContainsKey("$select"))
                {
                    request.QueryString = request.QueryString.Add("$select", string.Join(",", type.GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null).Select(p => p.GetCustomAttribute<DataMemberAttribute>().Name)));
                }
                var odataContext = new ODataQueryContext(context.Manager.Model, type, new Microsoft.OData.UriParser.ODataPath());
                IODataFeature odataFeature = request.HttpContext.ODataFeature();
                odataFeature.RoutePrefix = "/api/";

                odataContext.DefaultQuerySettings.EnableFilter = true;
                odataContext.DefaultQuerySettings.EnableExpand = true;
                odataContext.DefaultQuerySettings.EnableSelect = true;
                odataContext.DefaultQuerySettings.EnableCount = true;
                odataContext.DefaultQuerySettings.EnableSkipToken = true;

                var odata = new ODataQueryOptions(odataContext, request);

                metadataQuerySet = odata.ApplyTo(metadataQuerySet);

            }


            var items = await ((IQueryable<object>)metadataQuerySet).ToListAsync();
            //Console.WriteLine(metadataQuerySet.ToQueryString());
            //logger.LogTrace(metadataQuerySet.ToQueryString());


            //TODO - dotnet 5 and the use of system.text.json might be able to use internal clases of converts for all those types here.
            //annoying that we have to serialize them ourself.
            var resultList = new List<object>();

            foreach (var item in items)
            {
                if (item is DynamicEntity)
                {
                    resultList.Add(item);
                }
                else
                {
                    var converter = _factory.CreateConverter(item.GetType());
                    resultList.Add(converter.Convert(item));
                }


            }
            var odatafeature = request.ODataFeature();

            return new PageResult<object>(resultList, null, odatafeature.TotalCount);


            // return metadataQuerySet;
            // var setMethod=typeof(DbContext).GetMethod("Set", new Type[] { });
            //  return setMethod.MakeGenericMethod(type.dto).Invoke(this,new object[] { }) as IQueryable;
            //  return Set<Type2>();
        }
    }
    public interface IHasModelCacheKey
    {
        public string ModelCacheKey { get; }
    }
    public class DynamicContextModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
           => context is IHasModelCacheKey dynamicContext
               ? (context.GetType(), dynamicContext.ModelCacheKey, designTime)
               : (object)context.GetType();

        public object Create(DbContext context)
            => Create(context, false);
    }
    public class DynamicContext : DbContext, IDynamicContext, IHasModelCacheKey
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;
        private readonly IMigrationManager manager;
        private readonly ILogger logger;

        private const string MigrationDefaultName = "Initial";

        public string ModelCacheKey { get; set; } = Guid.NewGuid().ToString();

        public IMigrationManager Manager  => manager;

        protected DynamicContext(DbContextOptions options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager, ILogger logger)
          : base(options)


        {
            this.modelOptions = modelOptions;
            this.manager = migrationManager;
            this.logger = logger;
              
        }

       
        

        public DynamicContext(DbContextOptions<DynamicContext> options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager, ILogger<DynamicContext> logger)
        : this(options as DbContextOptions, modelOptions, migrationManager, logger as ILogger)


        {
            ChangeTracker.LazyLoadingEnabled = false;


        }
        public MigrationsInfo GetMigrations()
        {
            var types = new Dictionary<string, TypeInfo>
            {

            };
            var factories = new Dictionary<TypeInfo, Func<Migration>>();

            //if(modelOptions.Value.Manifests.Any())
            //{
            //    var migration = modelOptions.Value.Manifests.First();
            //    var name = $"{modelOptions.Value.PublisherPrefix}_{migration.SelectToken("$.version") ?? MigrationDefaultName}";
            //    var model = manager.CreateModel(name, migration, this.modelOptions.Value);

            //    types.Add(name, model.Item1);
            //    factories.Add(model.Item1, model.Item2);
            //}
            var latestManifest = modelOptions.Value.Manifests.First();
          //  var version = latestManifest.SelectToken("$.version")?.ToString().Replace(".", "_") ?? MigrationDefaultName;

            manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_latest", latestManifest, this.modelOptions.Value);

            if (modelOptions.Value.EnableDynamicMigrations)
            {
                int i = 0;
                foreach (var migration in modelOptions.Value.Manifests
                    .Select((m,i) => (target:m, source: i+1 == modelOptions.Value.Manifests.Length? new JObject(): modelOptions.Value.Manifests[i+1]))

                    .Reverse())
                {
                    
                    var name = $"{modelOptions.Value.PublisherPrefix}_{migration.target.SelectToken("$.version")?.ToString().Replace(".","_") ?? MigrationDefaultName}";
                   
                    var model = manager.CreateMigration(name, migration.target,migration.source, this.modelOptions.Value);

                    types.Add($"{++i:D16}{name}", model.Type);
                    factories.Add(model.Type, model.MigrationFactory);
                }
            }
            return new MigrationsInfo {  Factories = factories, Types = types};

            
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
            if (!optionsBuilder.IsConfigured)
            {
                EnsureModelCreated();
            }

           // optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
        }

     
        public ModelDefinition EnsureModelCreated()
        {

            var manifest = modelOptions.Value.Manifests.First();
            return manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_{manifest.SelectToken("$.version") ?? MigrationDefaultName}", manifest, this.modelOptions.Value);
        }

        public void AddNewManifest(JToken manifest)
        {
            this.modelOptions.Value.Manifests = new[] { manifest }.Concat(this.modelOptions.Value.Manifests).ToArray();
            ResetMigrationsContext();
        }

        public void ResetMigrationsContext()
        {
            ModelCacheKey = Guid.NewGuid().ToString();
            if (manager is MigrationManager man)
            {
                man.Reset(this.modelOptions.Value);
            }
            //var miassemb = Database.GetInfrastructure().GetRequiredService<IMigrationsAssembly>();
            //if (miassemb is DbSchemaAwareMigrationAssembly mya)
            //{ 
            //    mya.Reset(); 
            //}
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("Test");   
            var sw = Stopwatch.StartNew();
    
            //  EnsureModelCreated();
            if (this.modelOptions.Value.CreateLatestMigration)
            {
                var latestManifest = modelOptions.Value.Manifests.First();
             //   var version = latestManifest.SelectToken("$.version")?.ToString().Replace(".", "_") ?? MigrationDefaultName;

                manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_latest", modelOptions.Value.Manifests.First(), this.modelOptions.Value);
            }

            foreach (var en in manager.ModelDefinition.EntityDTOs)
            {
                try
                {
                    var a = modelBuilder.Entity(en.Value);
                    var config = Activator.CreateInstance(manager.ModelDefinition.EntityDTOConfigurations[en.Key]) as IEntityTypeConfiguration;
                    config.Configure(a);
                    foreach(var prop in a.Metadata.GetProperties().Where(c=>(Nullable.GetUnderlyingType( c.ClrType) ?? c.ClrType) == typeof(DateTime)))
                    {
                        prop.SetValueConverter(UtcValueConverter.Instance);
                    }
                }catch(Exception ex)
                {
                    Console.WriteLine($"Failed to configure: { en.Key}: { en.Value.FullName}");
                    logger.LogWarning(ex, "Failed to configure: {Model}: {Class}", en.Key, en.Value.FullName);
                    throw;
                }
                 
            }


            logger.LogTrace("Model Builded in {time}", sw.Elapsed);

            //  modelBuilder.ApplyConfiguration(new DynamicConfig());

            //  modelBuilder.ApplyConfigurationsFromAssembly(manager.Assembly);

            //var manifest = modelOptions.Value.Manifests.FirstOrDefault();
            //if(manifest != null)
            //{
            //    foreach(var entityDefinition in manifest.SelectToken("$.entities").OfType<JProperty>())
            //    {
            //        var EntitySchameName = entityDefinition.Name.Replace(" ", "");
            //        var EntityCollectionSchemaName = (entityDefinition.Value.SelectToken("$.pluralName")?.ToString() ?? EntitySchameName).Replace(" ", "");

            //        TypeBuilder entityType =
            //            myModule.DefineType(entitySchameName, TypeAttributes.Public);



            //        var dfc = entityType.DefineDefaultConstructor(MethodAttributes.Public);


            //        var entity = _entities.GetOrAdd(entityDefinition.Name, (n) => new DynamicEntity(entityDefinition));
            //        entity.ApplyConfiguration(modelBuilder);
            //    }


            //} 
            //  modelBuilder.GetType().GetMethod(nameof(ModelBuilder.ApplyConfiguration)).MakeGenericMethod(manager.EntityDTOs["Donor"].dto)
            //      .Invoke(modelBuilder,new object[] { Activator.CreateInstance(manager.EntityDTOs["Donor"].config)});
            //modelBuilder.ApplyConfiguration

            //modelBuilder.ApplyConfiguration(new DynamicEntity<Type2>());

            //foreach (var metadataEntity in _metaDataEntityList)
            //{
            //    modelBuilder.Entity(metadataEntity.EntityType).ToTable(metadataEntity.TableName, metadataEntity.SchemaName).HasKey("Id");

            //    foreach (var metaDataEntityProp in metadataEntity.Properties)
            //    {
            //        if (!metaDataEntityProp.IsNavigation)
            //        {
            //            var propBuilder = modelBuilder.Entity(metadataEntity.EntityType).Property(metaDataEntityProp.Name);

            //            if (!string.IsNullOrEmpty(metaDataEntityProp.ColumnName))
            //                propBuilder.HasColumnName(metaDataEntityProp.ColumnName);
            //        }
            //    }
            //}

            base.OnModelCreating(modelBuilder);
        }

        //public DbSet<DynamicEntity> Set(string entityCollectionSchemaName)
        //{
        //    var type = manager.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];//typeof(DonorDTO);//

        //    var metadataQuerySet = (DbSet<DynamicEntity>)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);
        //    return metadataQuerySet;
        //}
        
        public IQueryable Set(Type type)
        {
            return (IQueryable)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);
        }

        

        
       
      

        
        public EntityEntry Add(string entityName, JToken data)
        {
            var type = manager.ModelDefinition.EntityDTOs[entityName];
            var record = data.ToObject(type);
            logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            var a= this.Attach(record);
            a.State = EntityState.Added;
            return a;

        }

        public async Task<EntityEntry> AddOrReplace(string entityName, JToken data)
        {
            var type = manager.ModelDefinition.EntityDTOs[entityName];
            var record = data.ToObject(type);

            var keys = this.Model.FindEntityType(type).FindPrimaryKey().Properties.Select(c => (data as JObject).GetValue(c.PropertyInfo.GetCustomAttribute<DataMemberAttribute>()?.Name,StringComparison.OrdinalIgnoreCase)?.ToObject(c.PropertyInfo.PropertyType)).ToArray();
            //  this.Set(entityName).FindAsync()
          
            
            var db = await this.FindAsync(type, keys);
            if(db==null)
            {
                return this.Add(record);
            }
            else
            {
                var entry = this.Entry(db);
                entry.State = EntityState.Detached;
                return this.Update(record);
            }
 

        }

        public EntityEntry Remove(string entityName, JToken data)
        {
            var type = manager.ModelDefinition.EntityDTOs[entityName];
            var record = data.ToObject(type);

            return this.Remove(record);

        }
        public void Replace(string entityName, object entry, JToken data)
        {
            var type = manager.ModelDefinition.EntityDTOs[entityName];
            var record = data.ToObject(type);

            Entry(entry).CurrentValues.SetValues(record);

            // logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            // return this.Add(record);

        }



        public ValueTask<object> FindAsync(string entityName, params object[] keyValues)
        {

            if (!manager.ModelDefinition.EntityDTOs.ContainsKey(entityName))
            {
              throw new KeyNotFoundException($"The requested {entityName} was not part of model: {string.Join(", ", manager.ModelDefinition.EntityDTOs.Keys)}");
            }

            var type = manager.ModelDefinition.EntityDTOs[entityName];
            //  var record = data.ToObject(type);
            return this.FindAsync(type, keyValues);
            //  logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            // return this.Add(record);

        }
        public Type GetEntityType(string entityName)
        {
             var type = manager.ModelDefinition.EntityDTOs[entityName];
            return type;
        }
        public EntityEntry Update(string entityName, JToken data)
        {
            var type = manager.ModelDefinition.EntityDTOs[entityName];
            var record = data.ToObject(type);
            logger.LogInformation("Updating {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));

           
            var entity= this.Update(record);
 
            foreach(var prop in entity.Properties)
            {
                var logicalName = prop.Metadata.PropertyInfo.GetCustomAttribute<DataMemberAttribute>()?.Name;
                if(!string.IsNullOrEmpty(logicalName) && !prop.Metadata.IsPrimaryKey())
                    prop.IsModified = data[logicalName] != null;
            }


            foreach (var collection in entity.Collections)
            {
                var attr = collection.Metadata.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                var deletedItems = data[$"{attr.PropertyName}@deleted"];
                if (deletedItems != null)
                {
                    foreach(var id in deletedItems)
                    {
//#if NET5_0
                        var related=Activator.CreateInstance(collection.Metadata.TargetEntityType.ClrType);
                        //var keys = collection.Metadata.TargetEntityType.GetKeys();
                        //var primary = collection.Metadata.TargetEntityType.FindPrimaryKey();

                        //var a = primary.GetPrincipalKeyValueFactory<Guid>().CreateFromKeyValues(new object[] { id.ToObject<Guid>() });

                        collection.Metadata.TargetEntityType.ClrType.GetProperty("Id").SetValue(related, id.ToObject<Guid>());
//#else
//                        var targetType = collection.Metadata.GetTargetType();
//                        var related = Activator.CreateInstance(targetType.ClrType);
//                        //var keys = collection.Metadata.TargetEntityType.GetKeys();
//                        //var primary = collection.Metadata.TargetEntityType.FindPrimaryKey();

//                        //var a = primary.GetPrincipalKeyValueFactory<Guid>().CreateFromKeyValues(new object[] { id.ToObject<Guid>() });

//                        targetType.ClrType.GetProperty("Id").SetValue(related, id.ToObject<Guid>());
//#endif



                        Attach(related);
                        Remove(related);
                    }
                }

            }
            return entity;

        }

       
    }
}
