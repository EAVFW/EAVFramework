using EAVFramework.Endpoints;
using EAVFramework.Endpoints.Query;
using EAVFramework.Endpoints.Query.OData;
using EAVFramework.Extensions;
using EAVFramework.Shared;
using EAVFW.Extensions.Manifest.SDK;
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

        public static  Task<PageResult<object>> ExecuteHttpRequest<TContext>(this EAVDBContext<TContext> context, string entityCollectionSchemaName, string sql, HttpRequest request, params object[] sqlparams) where TContext : DynamicContext
        {
            List<IQueryExtender<TContext>> queryInspectors = GetQueryInspectors<TContext>(request);

            context.Context.EnsureModelCreated();

            var type = context.Context.Manager.ModelDefinition.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];
  
            var metadataQuerySet = context.FromSqlRaw<DynamicEntity>(type, sql, sqlparams) as IQueryable;
            
            return Execute<TContext>(request, type, context.Context, metadataQuerySet);

          
        }

        private static List<IQueryExtender<TContext>> GetQueryInspectors<TContext>(HttpRequest request) where TContext : DynamicContext
        {
            var t1 = typeof(IQueryExtender<>).MakeGenericType(typeof(TContext));
            var t2 = typeof(IEnumerable<>).MakeGenericType(t1);
            var queryInspectors = (request.HttpContext.RequestServices.GetService(t2) as IEnumerable).Cast<IQueryExtender<TContext>>()
                .ToList();
            return queryInspectors;
        }

        public static Task<PageResult<object>> ExecuteHttpRequest<TContext>(this TContext context, string entityCollectionSchemaName, HttpRequest request) where TContext : DynamicContext
        {
           

            context.EnsureModelCreated();

            var type = context.Manager.ModelDefinition.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];

            var metadataQuerySet = context.Set(type);

            return Execute<TContext>(request,type,context,metadataQuerySet);
           
        }
        public static async Task<PageResult<object>> Execute<TContext>(HttpRequest request, Type type, TContext context, IQueryable metadataQuerySet) where TContext : DynamicContext
        {
            List<IQueryExtender<TContext>> queryInspectors = GetQueryInspectors<TContext>(request);

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
                if (!request.Query.ContainsKey("$select") && !request.Query.ContainsKey("$apply"))
                {
                    request.QueryString = request.QueryString.Add("$select", string.Join(",", type.GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null && p.GetCustomAttribute<InversePropertyAttribute>() == null).Select(p => p.GetCustomAttribute<DataMemberAttribute>().Name)));
                }
                var odataContext = new ODataQueryContext(context.Manager.Model, type, new Microsoft.OData.UriParser.ODataPath());
                IODataFeature odataFeature = request.HttpContext.ODataFeature();
                odataFeature.RoutePrefix = "/api/";

                odataContext.DefaultQueryConfigurations.EnableFilter = true;
                odataContext.DefaultQueryConfigurations.EnableExpand = true;
                odataContext.DefaultQueryConfigurations.EnableSelect = true;
                odataContext.DefaultQueryConfigurations.EnableCount = true;
                odataContext.DefaultQueryConfigurations.EnableSkipToken = true;

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

        public IMigrationManager Manager => manager;

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

            manager.EnusureBuilded($"{modelOptions.Value.Schema}_latest", latestManifest, this.modelOptions.Value);

            if (modelOptions.Value.EnableDynamicMigrations)
            {
                int i = 0;
                foreach (var migration in modelOptions.Value.Manifests
                    .Select((m, i) => (target: m, source: i + 1 == modelOptions.Value.Manifests.Length ? new JObject() : modelOptions.Value.Manifests[i + 1]))

                    .Reverse())
                {

                    var name = $"{modelOptions.Value.Schema}_{migration.target.SelectToken("$.version")?.ToString().Replace(".", "_") ?? MigrationDefaultName}";

                    var model = manager.CreateMigration(name, migration.target, migration.source, this.modelOptions.Value);

                    types.Add($"{++i:D16}{name}", model.Type);
                    factories.Add(model.Type, model.MigrationFactory);
                }
            }
            return new MigrationsInfo { Factories = factories, Types = types };


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
            return manager.EnusureBuilded($"{modelOptions.Value.Schema}_latest", manifest, this.modelOptions.Value);
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
            
            var sw = Stopwatch.StartNew();

            //  EnsureModelCreated();
            if (this.modelOptions.Value.CreateLatestMigration)
            {
                var latestManifest = modelOptions.Value.Manifests.First();
                //   var version = latestManifest.SelectToken("$.version")?.ToString().Replace(".", "_") ?? MigrationDefaultName;

                manager.EnusureBuilded($"{modelOptions.Value.Schema}_latest", modelOptions.Value.Manifests.First(), this.modelOptions.Value);
            }

            foreach (var en in manager.ModelDefinition.EntityDTOs)
            {
                try
                {
                    var a = modelBuilder.Entity(en.Value);
                    var config = Activator.CreateInstance(manager.ModelDefinition.EntityDTOConfigurations[en.Key]) as IEntityTypeConfiguration;
                    config.Configure(a);
                    foreach (var prop in a.Metadata.GetProperties().Where(c => (Nullable.GetUnderlyingType(c.ClrType) ?? c.ClrType) == typeof(DateTime)))
                    {
                        prop.SetValueConverter(UtcValueConverter.Instance);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to configure: {en.Key}: {en.Value.FullName}");
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


            //Handling Polylookups (Split mode)
            if (manager.ModelDefinition.Entities.ContainsKey(entityName))
            {
                var entity = manager.ModelDefinition.Entities[entityName];
                foreach(var poly in entity.Attributes.Where(a=>a.Value is AttributeObjectDefinition typeobj && typeobj.AttributeType.Type == "polylookup" && typeobj.AttributeType.Split))
                {
                    var attr = poly.Value as AttributeObjectDefinition;
                    var reference = data[attr.LogicalName]?.ToString();

                    if (!string.IsNullOrEmpty(reference))
                    {
                        var referenceType = reference.Substring(0, reference.IndexOf(':'));
                        var referenceId = reference.Substring(referenceType.Length+1);
                        data[$"{entity.LogicalName}{referenceType}references"] = new JArray(
                            new JObject(
                                new JProperty($"{referenceType}id", referenceId)
                                )
                            );
                    }
                }
            }


            var record = data.ToObject(type);
            logger.LogInformation("Adding {CLRType} from {rawData} to {typedData}", type.Name, data.ToString(), JsonConvert.SerializeObject(record));
            var a = this.Attach(record);
            a.State = EntityState.Added;
            return a;

        }

        public async Task<EntityEntry> AddOrReplace(string entityName, JToken data)
        {
            var type = manager.ModelDefinition.EntityDTOs[entityName];
            var record = data.ToObject(type);

            var keys = this.Model.FindEntityType(type).FindPrimaryKey().Properties.Select(c => (data as JObject).GetValue(c.PropertyInfo.GetCustomAttribute<DataMemberAttribute>()?.Name, StringComparison.OrdinalIgnoreCase)?.ToObject(c.PropertyInfo.PropertyType)).ToArray();
            //  this.Set(entityName).FindAsync()


            var db = await this.FindAsync(type, keys);
            if (db == null)
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


            var entity = this.Update(record);

            foreach (var prop in entity.Properties)
            {
                var logicalName = prop.Metadata.PropertyInfo.GetCustomAttribute<DataMemberAttribute>()?.Name;
                if (!string.IsNullOrEmpty(logicalName) && !prop.Metadata.IsPrimaryKey())
                    prop.IsModified = data[logicalName] != null;
            }


            foreach (var collection in entity.Collections)
            {
                var attr = collection.Metadata.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                var deletedItems = data[$"{attr.PropertyName}@deleted"];
                if (deletedItems != null)
                {
                    foreach (var id in deletedItems)
                    {
                        //#if NET5_0
                        var related = Activator.CreateInstance(collection.Metadata.TargetEntityType.ClrType);
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
