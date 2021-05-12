using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetDevOps.Extensions.EAVFramwork
{
    public class DynamicContext : DbContext, IDynamicContext
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;
        private readonly IMigrationManager manager;

        //private readonly MigrationManager manager = new MigrationManager();




        public DynamicContext(DbContextOptions options, IOptions<DynamicContextOptions> modelOptions, IMigrationManager migrationManager)
            : base(options)


        {
            this.modelOptions = modelOptions;
            this.manager = migrationManager;
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
            optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
        }

      //  public List<MetadataEntity> _metaDataEntityList = new List<MetadataEntity>();

        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

          

            foreach (var en in manager.EntityDTOs)
            {
                var a = modelBuilder.Entity(en.Value.dto);
                var config = Activator.CreateInstance(en.Value.config) as IEntityTypeConfiguration;
                config.Configure(a);
            }


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

        public IQueryable<DynamicEntity> Set(string entityName)
        {

            // return this.Query
            var type = manager.EntityDTOs[entityName].dto;//typeof(DonorDTO);//
             
            var methoda = this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type);
           // var methodb = this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(typeof(DonorDTO));
            var a= methoda.Invoke(this, new object[0]) as IQueryable;
            // var b = methodb.Invoke(this, new object[0]) as IQueryable;
         
            var metadataQuerySet = (IQueryable<DynamicEntity>)this.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type).Invoke(this, null);

            

            return metadataQuerySet;
            // var setMethod=typeof(DbContext).GetMethod("Set", new Type[] { });
            //  return setMethod.MakeGenericMethod(type.dto).Invoke(this,new object[] { }) as IQueryable;
            //  return Set<Type2>();
        }

        public EntityEntry Add(string entityName, JToken data)
        {
            var type = manager.EntityDTOs[entityName].dto;

            return this.Add(data.ToObject(type));

        }

    }
}
