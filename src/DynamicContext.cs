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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
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

namespace DotNetDevOps.Extensions.EAVFramework
{

    public interface IQueryExtender
    {
        IQueryable ApplyTo(IQueryable metadataQuerySet, DynamicContext context, Type type, HttpRequest request);
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

    internal interface IODataConverter
    {
        object Convert(object data);

    }
    internal interface IODataConverterFactory
    {
        IODataConverter CreateConverter(Type type);
    }
    internal class SelectCoverter : IODataConverter
    {
        private Type type;
        private IODataConverterFactory odatatConverterFactory;
        private MethodInfo entityProperty;
        private object MapperProvider;
        //private readonly string serializedType;
        public SelectCoverter(Type type, IODataConverterFactory odatatConverterFactory)
        {
            this.type = type;
            this.odatatConverterFactory = odatatConverterFactory;
            this.entityProperty = type.GetMethod("ToDictionary", new[] { typeof(Func<IEdmModel, IEdmStructuredType, IPropertyMapper>) });
            var SelectExpandWrapperConverter = type.Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapperConverter");
            this.MapperProvider = SelectExpandWrapperConverter.GetField("MapperProvider", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            //serializedType = $"{type.GetGenericArguments().First().FullName}, {type.GetGenericArguments().First().Assembly.GetName().Name}";
        }

        public object Convert(object data)
        {
            var poco = (IDictionary<string, object>)entityProperty.Invoke(data, new object[] { MapperProvider });
            //poco["$type"] = serializedType;
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
    internal class OdatatConverterFactory : IODataConverterFactory
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
    public class DynamicContext : DbContext, IDynamicContext
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;
        private readonly IMigrationManager manager;
        private readonly ILogger logger;

  
        protected DynamicContext(DbContextOptions options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager, ILogger logger)
          : base(options)


        {
            this.modelOptions = modelOptions;
            this.manager = migrationManager;
            this.logger = logger;

            this.ChangeTracker.LazyLoadingEnabled = false;
            
        }

        

        public DynamicContext(DbContextOptions<DynamicContext> options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager, ILogger<DynamicContext> logger)
        : this(options as DbContextOptions, modelOptions, migrationManager, logger as ILogger)


        {

        }
        public MigrationsInfo GetMigrations()
        {
            
            var name = $"{modelOptions.Value.PublisherPrefix}_Initial";
            var model = manager.CreateModel(name, modelOptions.Value.Manifests.First(), this.modelOptions.Value);
            return new MigrationsInfo
            {
                Types = new Dictionary<string, TypeInfo>
                {
                    [name] = model.Item1
                },
                Factories = new Dictionary<TypeInfo, Func<Migration>>
                {
                    [model.Item1] = model.Item2
                }
            };
        }

        //public virtual IReadOnlyDictionary<string, Migration> GetMigrations()
        //{
        //    return manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(), this.modelOptions.Value);
        //}

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
                manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_Initial",modelOptions.Value.Manifests.First(), this.modelOptions.Value);
            }

           // optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
        }

        //  public List<MetadataEntity> _metaDataEntityList = new List<MetadataEntity>();
        public void EnsureModelCreated()
        {
            manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(), this.modelOptions.Value);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("TEST");
            var sw = Stopwatch.StartNew();
            manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_Initial",modelOptions.Value.Manifests.First(), this.modelOptions.Value);
           // manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", );

            foreach (var en in manager.EntityDTOs)
            {
                var a = modelBuilder.Entity(en.Value);
                var config = Activator.CreateInstance(manager.EntityDTOConfigurations[en.Key]) as IEntityTypeConfiguration;
                config.Configure(a);
                // Console.WriteLine(a.Metadata.ToDebugString(Microsoft.EntityFrameworkCore.Infrastructure.MetadataDebugStringOptions.LongDefault));
                // Console.WriteLine(string.Join(",",a.Metadata.GetForeignKeys().Select(c=>c.GetConstraintName())));
                Console.WriteLine(en.Value.Name);

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

        public async Task<PageResult<object>> ExecuteHttpRequest(string entityCollectionSchemaName, HttpRequest request)
        {
            var queryInspector = request.HttpContext.RequestServices.GetService<IQueryExtender>();


            //  var migrations = GetMigrations(); //ensures that types are build
            manager.EnusureBuilded($"{modelOptions.Value.PublisherPrefix}_Initial",modelOptions.Value.Manifests.First(), this.modelOptions.Value);

            var type = manager.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];

            var metadataQuerySet = Set(type);
            metadataQuerySet = queryInspector?.ApplyTo(metadataQuerySet, this, type, request).Cast(type) ?? metadataQuerySet;




            if (request != null)
            {
                if (!request.Query.ContainsKey("$select"))
                {
                    request.QueryString = request.QueryString.Add("$select", string.Join(",", type.GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null).Select(p => p.GetCustomAttribute<DataMemberAttribute>().Name)));
                }
                var context = new ODataQueryContext(manager.Model, type, new Microsoft.OData.UriParser.ODataPath());
                IODataFeature odataFeature = request.HttpContext.ODataFeature();
                odataFeature.RoutePrefix =  "/api/";
              
                context.DefaultQuerySettings.EnableFilter = true;
                context.DefaultQuerySettings.EnableExpand = true;
                context.DefaultQuerySettings.EnableSelect = true;
                context.DefaultQuerySettings.EnableCount = true;
                context.DefaultQuerySettings.EnableSkipToken = true;

                var odata = new ODataQueryOptions(context, request);
              
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
        private static OdatatConverterFactory _factory = new OdatatConverterFactory();
        private static object ToPoco(object item)
        {
            if (item == null)
                return null;

            var converter = _factory.CreateConverter(item.GetType());
            return converter.Convert(item);



        }

        public Type GetRecordType(string entityName)
        {
            return manager.EntityDTOs[entityName];
        }
        public EntityEntry Add(string entityName, JToken data)
        {
            var type = manager.EntityDTOs[entityName];
            var record = data.ToObject(type);
            logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            return this.Add(record);

        }

        public async Task<EntityEntry> AddOrReplace(string entityName, JToken data)
        {
            var type = manager.EntityDTOs[entityName];
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
            var type = manager.EntityDTOs[entityName];
            var record = data.ToObject(type);

            return this.Remove(record);

        }
        public void Replace(string entityName, object entry, JToken data)
        {
            var type = manager.EntityDTOs[entityName];
            var record = data.ToObject(type);

            Entry(entry).CurrentValues.SetValues(record);

            // logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            // return this.Add(record);

        }



        public ValueTask<object> FindAsync(string entityName, params object[] keyValues)
        {

            if (!manager.EntityDTOs.ContainsKey(entityName))
            {
              throw new KeyNotFoundException($"The requested {entityName} was not part of model: {string.Join(", ", manager.EntityDTOs.Keys)}");
            }

            var type = manager.EntityDTOs[entityName];
            //  var record = data.ToObject(type);
            return this.FindAsync(type, keyValues);
            //  logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            // return this.Add(record);

        }
        public Type GetEntityType(string entityName)
        {
             var type = manager.EntityDTOs[entityName];
            return type;
        }
        public EntityEntry Update(string entityName, JToken data)
        {
            var type = manager.EntityDTOs[entityName];
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
