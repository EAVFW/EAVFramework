using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
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
        IQueryable ApplyTo(IQueryable metadataQuerySet, DynamicContext context, Type type);
    }

    public class DynamicContext : DbContext, IDynamicContext
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;
        private readonly IMigrationManager manager;
        private readonly ILogger logger;

       
        //private readonly MigrationManager manager = new MigrationManager();




        public DynamicContext(DbContextOptions options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager, ILogger<DynamicContext> logger)
            : base(options)


        {
            this.modelOptions = modelOptions;
            this.manager = migrationManager;
            this.logger = logger;

            this.ChangeTracker.LazyLoadingEnabled = false;
        }

        public virtual IReadOnlyDictionary<string, Migration> GetMigrations()
        {   
            return manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(),this.modelOptions.Value);
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
            var sw = Stopwatch.StartNew();

            manager.BuildMigrations($"{modelOptions.Value.PublisherPrefix}_Initial", modelOptions.Value.Manifests.First(), this.modelOptions.Value);

            foreach (var en in manager.EntityDTOs)
            {
                var a = modelBuilder.Entity(en.Value);
                var config = Activator.CreateInstance(manager.EntityDTOConfigurations[en.Key]) as IEntityTypeConfiguration;
                config.Configure(a);
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

            var methoda = this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type);
            // var methodb = this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(typeof(DonorDTO));
            var a = methoda.Invoke(this, new object[0]) as IQueryable;
            // var b = methodb.Invoke(this, new object[0]) as IQueryable;

            var metadataQuerySet = (IQueryable< DynamicEntity>)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);
            return metadataQuerySet;
        }
        public async Task<PageResult<object>> Set(string entityCollectionSchemaName,HttpRequest request)
        {
            var queryInspector = request.HttpContext.RequestServices.GetService<IQueryExtender>();

            var migrations = GetMigrations();
            // return this.Query
            var type = manager.EntityDTOs[entityCollectionSchemaName.Replace(" ", "")];//typeof(DonorDTO);//
             
            var methoda = this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type);
           // var methodb = this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(typeof(DonorDTO));
            var a= methoda.Invoke(this, new object[0]) as IQueryable;
            // var b = methodb.Invoke(this, new object[0]) as IQueryable;
         
            var metadataQuerySet = (IQueryable)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);

            metadataQuerySet = queryInspector?.ApplyTo(metadataQuerySet, this, type);

            if (request != null)
            {
               

                var context = new ODataQueryContext(manager.Model, type, new Microsoft.OData.UriParser.ODataPath());
                context.DefaultQuerySettings.EnableFilter = true;
                context.DefaultQuerySettings.EnableExpand = true;
                context.DefaultQuerySettings.EnableSelect = true;

                //                var Validator = new FilterQueryValidator(context.DefaultQuerySettings);
                metadataQuerySet = new ODataQueryOptions(context, request).ApplyTo(metadataQuerySet);

              
               // var _queryOptionParser = new ODataQueryOptionParser(
               //context.Model,
               //context.ElementType,
               //context.NavigationSource,
               //new Dictionary<string, string> { { "$filter", odataFilter }, {"$expand", "Tournament" } },
               //context.RequestContainer);

                // _queryOptionParser.ParseSelectAndExpand();
                // //var _filterClause = _queryOptionParser.ParseFilter();

                // // SingleValueNode filterExpression = _filterClause.Expression.Accept(
                // //       new ParameterAliasNodeTranslator(_queryOptionParser.ParameterAliasNodes)) as SingleValueNode;
                // // filterExpression = filterExpression ?? new ConstantNode(null);
                // // _filterClause = new FilterClause(filterExpression, _filterClause.RangeVariable);

                // var odatafilter = new FilterQueryOption(odataFilter,context, _queryOptionParser);

                // var expandoptions = new SelectExpandQueryOption(null, "Tournament", context, _queryOptionParser);
                // var valsettings = new Microsoft.AspNetCore.OData.Query.Validator.ODataValidationSettings() { AllowedFunctions = AllowedFunctions.AllFunctions };

                // expandoptions.Validate(valsettings);
                // odatafilter.Validate(valsettings);

                // var settings = new ODataQuerySettings { };
                // metadataQuerySet = (IQueryable<DynamicEntity>) expandoptions.ApplyTo( odatafilter.ApplyTo(metadataQuerySet, settings), settings);
            }
            // SelectExpandBinder
            // IQueryable <Microsoft.AspNetCore.OData.Query.Wrapper..SelectAllAndExpand<DynamicEntity>> a = ;

            var items = await ((IQueryable<object>)metadataQuerySet).ToListAsync();




             var resultList = new List<object>();

                foreach(var item in items)
                {
                    if (item is DynamicEntity)
                    {
                        resultList.Add(item);
                    }
                    else if (item.GetType().Name == "SelectAllAndExpand`1")
                    {
                        var entityProperty = item.GetType().GetProperty("Instance");
                        resultList.Add( entityProperty.GetValue(item));
                    }
                }
            return new PageResult<object>(resultList, null, null);
            

           // return metadataQuerySet;
            // var setMethod=typeof(DbContext).GetMethod("Set", new Type[] { });
            //  return setMethod.MakeGenericMethod(type.dto).Invoke(this,new object[] { }) as IQueryable;
            //  return Set<Type2>();
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
            logger.LogInformation("Updating {CLRType} from {rawData} to {typedData}",type.Name,data.ToString(),JsonConvert.SerializeObject(record));
            return this.Update(data.ToObject(type));

        }

    }
}
