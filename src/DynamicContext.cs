using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Container;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public class DynamicContext : DbContext, IDynamicContext
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;
        private readonly IMigrationManager manager;
        private readonly ILogger logger;


        //private readonly MigrationManager manager = new MigrationManager();




        public DynamicContext(DbContextOptions<DynamicContext> options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager, ILogger<DynamicContext> logger)
            : base(options)


        {
            this.modelOptions = modelOptions;
            this.manager = migrationManager;
            this.logger = logger;

            this.ChangeTracker.LazyLoadingEnabled = false;
        }

        public virtual IReadOnlyDictionary<string, Migration> GetMigrations()
        {
            return manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(), this.modelOptions.Value);
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
                manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(), this.modelOptions.Value);
            }

            optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
        }

        //  public List<MetadataEntity> _metaDataEntityList = new List<MetadataEntity>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("TEST");
            var sw = Stopwatch.StartNew();

            manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(), this.modelOptions.Value);

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

        public IQueryable<DynamicEntity> Set(string entityCollectionSchemaName)
        {
            var type = manager.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];//typeof(DonorDTO);//
  
            var metadataQuerySet = (IQueryable<DynamicEntity>)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);
            return metadataQuerySet;
        }
        public IQueryable Set(Type type)
        {
            return (IQueryable)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);
        }

        public async Task<PageResult<object>> ExecuteHttpRequest(string entityCollectionSchemaName, HttpRequest request)
        {
            var queryInspector = request.HttpContext.RequestServices.GetService<IQueryExtender>();



            var migrations = GetMigrations(); //ensures that types are build
  
            var type = manager.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];
 
            var metadataQuerySet = Set(type);
            metadataQuerySet = queryInspector?.ApplyTo(metadataQuerySet, this, type, request).Cast(type) ?? metadataQuerySet;

          


            if (request != null)
            { 
                if(!request.Query.ContainsKey("$select"))
                {
                    request.QueryString = request.QueryString.Add("$select", string.Join(",", type.GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null).Select(p=>p.GetCustomAttribute<DataMemberAttribute>().Name)));
                }
                var context = new ODataQueryContext(manager.Model, type, new Microsoft.OData.UriParser.ODataPath());
                context.DefaultQuerySettings.EnableFilter = true;
                context.DefaultQuerySettings.EnableExpand = true;
                context.DefaultQuerySettings.EnableSelect = true;
           
                  
                metadataQuerySet = new ODataQueryOptions(context, request).ApplyTo(metadataQuerySet);
                 
            }
           

            var items = await ((IQueryable<object>)metadataQuerySet).ToListAsync();
            Console.WriteLine(metadataQuerySet.ToQueryString());
            logger.LogTrace(metadataQuerySet.ToQueryString());


            //TODO - dotnet 5 and the use of system.text.json might be able to use internal clases of converts for all those types here.
            //annoying that we have to serialize them ourself.
            var resultList = new List<object>();

            foreach (var item in items)
            {
                if (item is DynamicEntity)
                {
                    resultList.Add(item);
                }
                else if (item.GetType().Name == "SelectAllAndExpand`1")
                {
                    var entityProperty = item.GetType().GetProperty("Instance");
                    if (entityProperty.GetType().Name == "SelectSome`1")
                    {

                        resultList.Add(ToPoco(entityProperty.GetValue(item)));

                    }
                    else
                    {
                        resultList.Add(entityProperty.GetValue(item));
                    }
                }
                else if (item.GetType().Name == "SelectSome`1")
                {
                    //value.ToDictionary(SelectExpandWrapperConverter.MapperProvider)
                    object poco = ToPoco(item);
                    resultList.Add(poco);
                }
                else
                {

                    throw new InvalidCastException("Unknown Type: " + item.GetType().Name);
                }
            }
            return new PageResult<object>(resultList, null, null);


            // return metadataQuerySet;
            // var setMethod=typeof(DbContext).GetMethod("Set", new Type[] { });
            //  return setMethod.MakeGenericMethod(type.dto).Invoke(this,new object[] { }) as IQueryable;
            //  return Set<Type2>();
        }

        private static object ToPoco(object item)
        {
            if (item == null)
                return null;

            if (item.GetType().Name == "SelectAllAndExpand`1")
            {
                var entityProperty = item.GetType().GetProperty("Instance");
                return ToPoco(entityProperty.GetValue(item));
            }
            else if (item.GetType().Name == "SelectSome`1" || item.GetType().Name == "SelectAll`1" || item.GetType().Name == "SelectSomeAndInheritance`1")
            {

                var entityProperty = item.GetType().GetMethod("ToDictionary", new[] { typeof(Func<IEdmModel, IEdmStructuredType, IPropertyMapper>) });
                var SelectExpandWrapperConverter = item.GetType().Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapperConverter");
                var poco = (IDictionary<string, object>)entityProperty.Invoke(item, new object[] { SelectExpandWrapperConverter.GetField("MapperProvider", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) });

                foreach (var kv in poco.ToArray())
                {
                    poco[kv.Key] = ToPoco(kv.Value);
                }

                return poco;
            }
            else if (typeof(IEnumerable).IsAssignableFrom(item.GetType()) && !(item is string))
            {
               
                var list = new List<object>();
                foreach(var i in item as IEnumerable)
                {
                    list.Add(ToPoco(i));
                }
                return list;
            }
            else
            {
                Console.WriteLine(item.GetType().Name);
                
            }

            return item;
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
            var type = manager.EntityDTOs[entityName];
            //  var record = data.ToObject(type);
            return this.FindAsync(type, keyValues);
            //  logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            // return this.Add(record);

        }

        public EntityEntry Update(string entityName, JToken data)
        {
            var type = manager.EntityDTOs[entityName];
            var record = data.ToObject(type);
            logger.LogInformation("Updating {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            return this.Update(data.ToObject(type));

        }

    }
}
