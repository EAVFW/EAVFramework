using EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Reflection;
using EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EAVFramework.Endpoints
{
    public class DataUrlHelper
    {
        public string Name { get; set; }

        public string ContentType { get; set; }

        public byte[] Data { get; set; }

        public void Parse(string dataUrl)
        {
            if (!dataUrl.StartsWith("data:"))
            {
                Data = Convert.FromBase64String(dataUrl);
                return;
            }


            var matches = Regex.Match(dataUrl, @"data:(?<type>.+?);name=(?<name>.+?);base64,(?<data>.+)");

            if (matches.Groups.Count < 3)
            {
                throw new Exception("Invalid DataUrl format");
            }

            ContentType = matches.Groups["type"].Value;
            Name= matches.Groups["name"].Value; ;
            Data =  Convert.FromBase64String(matches.Groups["data"].Value);
        }
    }

    public class DataUrlConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }



        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var a = new DataUrlHelper();
            a.Parse(reader.ReadAsString());

            return a.Data;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class EAVDBContext 
    {
        public abstract Task SaveChangesAsync(ClaimsPrincipal user);
        public abstract ValueTask<EntityEntry> FindAsync(string entityName, params object[] keys);
        public abstract EntityEntry Add(string entityName, JToken record);
    }

    public record EntityRef([property: JsonProperty("$type")] string Type, [property: JsonProperty("id")] Guid Id);

    public class EAVDBContext<TContext> : EAVDBContext
        where TContext : DynamicContext
    {
        public TContext Context { get; }

        private readonly PluginsAccesser<TContext> plugins;
        private readonly ILogger<EAVDBContext<TContext>> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IPluginScheduler<TContext> pluginScheduler;


        public T GetContextService<T>() where T : class
        {
            return this.Context.Database.GetService<T>();
        }

        public IRelationalTransactionManager RelationalTransactionManager => GetContextService<IDbContextTransactionManager>() as IRelationalTransactionManager;

        public async ValueTask<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
        {
            // Note: transactions that specify an explicit isolation level are only supported by
            // relational providers and trying to use them with a different provider results in
            // an invalid operation exception being thrown at runtime. To prevent that, a manual
            // check is made to ensure the underlying transaction manager is relational.

            try
            {
                return await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            }

            catch(InvalidOperationException)
            {
                return null;
            }
        }

        public virtual EntityEntry Remove(object obj)
        {
            return this.Context.Remove(obj);
        }
        public virtual EntityEntry Add(object obj)
        {
            return this.Context.Add(obj);
        }
         
        public virtual EntityEntry RemoveWithoutCascading(object obj)
        {
            var entry = this.Context.Entry(obj);
            entry.State = EntityState.Deleted;
            return entry;
        }
        public virtual EntityEntry Entry(object obj)
        {
            return this.Context.Entry(obj);
        }
        public virtual EntityEntry<T> Entry<T>(T obj) where T : class
        {
            return this.Context.Entry<T>(obj);
        }

        public void ResetEntryTracking(object obj)
        {
            Context.Entry(obj).State = EntityState.Unchanged;
        }
        public void ResetEntryTracking(IEnumerable<object> objs)
        {
            foreach (var obj in objs)
            {
                Context.Entry(obj).State = EntityState.Unchanged;
            }
        }
        public EntityEntry Attach(object obj) {
            return Context.Attach(obj);
        }
        public EntityEntry Update(object obj)
        {
            return Context.Update(obj);
        }

        //   private static JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {  Converters = { new DataUrlConverter } });
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public void ResetMigrationsContext()
        {
            Context.ResetMigrationsContext();
        }

        public async Task<T> ExecuteTaskAsync<T>(Func<Task<T>> query, CancellationToken cancellationToken = default)
        {

            try
            {
                await semaphoreSlim.WaitAsync(cancellationToken);

                return await query();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        public async ValueTask<T> ExecuteValueTaskAsync<T>(Func<ValueTask<T>> query, CancellationToken cancellationToken = default)
        {

            try
            {
                await semaphoreSlim.WaitAsync(cancellationToken);

                return await query();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public ValueTask<T> FindAsync<T>(params object[] keys)
            where T : DynamicEntity
        {
            return this.Context.FindAsync<T>(keys);
        }

         

        public EAVDBContext(
            TContext context, 
            PluginsAccesser<TContext> plugins, 
            ILogger<EAVDBContext<TContext>> logger, 
            IServiceProvider serviceProvider, IPluginScheduler<TContext> pluginScheduler)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.plugins = plugins;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.pluginScheduler = pluginScheduler;
            context.EnsureModelCreated();
        }
        
        public async Task MigrateAsync()
        {
            var migrator = Context.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sqlscript = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            logger.LogInformation("Migrating: {SQL}", sqlscript);
           // await migrator.MigrateAsync();

            var conn = Context.Database.GetDbConnection();
          
            if(conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            foreach (var sql in sqlscript.Split("GO"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                //  await context.Context.Database.ExecuteSqlRawAsync(sql);


                var r = await cmd.ExecuteNonQueryAsync();
            }


        }
        public async ValueTask<JToken> ReadRecordAsync(HttpContext context, ReadOptions options)
        {
            if (options.LogPayload)
            {
                var reader = new StreamReader(context.Request.BodyReader.AsStream());
                var text = await reader.ReadToEndAsync();
                logger.LogInformation("Reading Payload : {Payload}", text);

                var record = JToken.Parse(text);
                if (!string.IsNullOrEmpty(options.RecordId))
                    record["id"] =  options.RecordId;
                return record;
            }
            else
            {

                var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream())));
                if (!string.IsNullOrEmpty(options.RecordId))
                    record["id"] = options.RecordId;
                return record;
            }
        }

        public EAVResource CreateEAVResource(string entityName, HttpContext context)
        {
            var type = Context.GetEntityType(entityName);
            return new EAVResource
            {
                EntityType = type,
                EntityCollectionSchemaName =  type.GetCustomAttribute<EntityAttribute>().CollectionSchemaName,
                Host =context.Request.Host
            };
        }

        public ValueTask<OperationContext<TContext>> SaveChangesAsync(ClaimsPrincipal user, Func<OperationContext<TContext>, Task> onBeforeCommit = null)
        {
            return this.Context.SaveChangesPipeline(serviceProvider, user, plugins, pluginScheduler, onBeforeCommit);
        }
        
        public override async Task SaveChangesAsync(ClaimsPrincipal user)
        {
            var a= await this.Context.SaveChangesPipeline(serviceProvider, user, plugins, pluginScheduler);


           if(a.Errors.Any())
            throw new Exception("Failed to save, " + String.Join(",", a.Errors.Select(c => c.Error)));
        }

        static ConcurrentDictionary<string, PropertyInfo> methods = new ConcurrentDictionary<string, PropertyInfo>();

        public async ValueTask<EntityEntry> PatchAsync(string entityName, Guid recordId, JToken record)
        {
            //   this.Context.ChangeTracker.LazyLoadingEnabled=true;
            var entity = await FindAsync(entityName, recordId);


            /**
             * We load up all properties with an object from database unless no id on object is provided,
             * then its assumed to be a new object or partial update
             * Two Cases:
             * 1: if the object has no id field but the root object do have propertyname + "id", then it should be partial update
             * 2: if no id and no propertyname + "id" field on root, then its assume that the object should replace the existing as new
             */
            var relatedProps = record.OfType<JProperty>()
                .Where(p => p.Value.Type == JTokenType.Object && p.Value is JObject obj && (obj.ContainsKey("id")|| record[p.Name+"id"] !=null )).ToArray();

            foreach (var related in relatedProps.Select(related =>
                 entity.References.FirstOrDefault(c => string.Equals(c.Metadata.Name, related.Name, StringComparison.OrdinalIgnoreCase))))
            {
                if (related!=null)
                {
                    await related.LoadAsync();
                }
            }


            var serializer = new JsonSerializer();

            await PopulateAsync(record, entity, serializer);

            entity.State = EntityState.Modified; //Always be modifed on the main entity to trigger plugins.

            // TraverseDeleteCollections(record, new Lazy<Type>(entity.Entity.GetType()));

            logger.LogInformation("Patching {EntityName}<{RecordId}>: {Changes}", entityName, recordId, this.Context.ChangeTracker.DebugView.LongView);

            return entity;
        }

        private async Task PopulateAsync(JToken record, EntityEntry entity, JsonSerializer serializer)
        {
            foreach (var prop in record.OfType<JProperty>())
            {
                var propName = prop.Name.EndsWith("@deleted") ? prop.Name.Substring(0, prop.Name.IndexOf('@')) : prop.Name;

                var method = methods.GetOrAdd(entity.Metadata.GetSchemaQualifiedTableName()+propName, (_) => entity.Entity.GetType().GetProperties().FirstOrDefault(c => c.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName == propName));

                
                if (method!=null)
                {
                    if (prop.Name.EndsWith("@deleted"))
                    {
                        // var collection = clrType.Value.GetProperties().FirstOrDefault(propertyInfo =>
                        //    propertyInfo.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == prop.Name.Substring(0, prop.Name.IndexOf('@')));

                        var deletedItems = prop.Value;

                        foreach (var id in deletedItems)
                        {

                            var related = Activator.CreateInstance(method.PropertyType.GenericTypeArguments[0]);

                            var entry = Context.Entry(related);

                            var p = entry.Metadata.FindPrimaryKey().Properties.SingleOrDefault();
                           
                            var deleteItem = id.ToString().Split("@");
                            p.PropertyInfo.SetValue(related, p.PropertyInfo.PropertyType == typeof(Guid) ? Guid.Parse(deleteItem[0]): throw new Exception("Primary type other than guid is not supported"));

                           
                            var ct = entry.Metadata.GetProperties().FirstOrDefault(c => c.IsConcurrencyToken);
                            if (ct != null)
                            {
                                if (deleteItem.Length > 1)
                                {

                                    ct.PropertyInfo.SetValue(related, Convert.FromBase64String(deleteItem[1]));
                                }
                                else
                                {
                                    await entry.ReloadAsync();
                                }
                            }

                                
                                Context.Attach(related);
                            Context.Remove(related);

                        }

                    }else if (prop.Value.Type == JTokenType.Object)
                    {

                        var existing = method.GetValue(entity.Entity);
                        if (existing==null)
                        {
                            existing=Activator.CreateInstance(method.PropertyType);
                            method.SetValue(entity.Entity, existing);
                        }


                        EntityEntry entry = Attach(serializer, prop.Value, existing);

                        await PopulateAsync(prop.Value, entry, serializer);

                        //Populate 


                    }
                    else if (prop.Value.Type == JTokenType.Array)
                    {
                        var collectionElementType = method.PropertyType.GetGenericArguments().First();
                        IList existingCollection = method.GetValue(entity.Entity) as IList;

                        if (existingCollection==null)
                        {
                            existingCollection=Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionElementType)) as IList;
                            method.SetValue(entity.Entity, existingCollection);
                        }

                        foreach (var obj in prop.Value)
                        {
                            var element = Activator.CreateInstance(collectionElementType);
                            existingCollection.Add(element);
                            EntityEntry entry = Attach(serializer, obj, element);
                            //Populate
                            await PopulateAsync(obj, entry, serializer);

                        }
                    }
                    else if(prop.Value.Type == JTokenType.Null)
                    {
                        if (method.PropertyType.IsGenericType && method.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                        {
                            var collectionElementType = method.PropertyType.GetGenericArguments().First();
                            IList existingCollection = Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionElementType)) as IList;

                            method.SetValue(entity.Entity, existingCollection);
                            //entity.Property(method.Name).IsModified = true;
                        }
                        else
                        {

                            method.SetValue(entity.Entity, serializer.Deserialize(prop.Value.CreateReader(), method.PropertyType));
                            entity.Property(method.Name).IsModified = true;
                        }
                    }
                    else
                    {
                        method.SetValue(entity.Entity, serializer.Deserialize(prop.Value.CreateReader(), method.PropertyType));

                    }
                }
                else
                {

                }
            }

            entity.DetectChanges();

        }

        private EntityEntry Attach(JsonSerializer serializer, JToken obj, object element)
        {
            var entry = Context.Entry(element);
            var hasKey = false;
            foreach (var p in entry.Metadata.FindPrimaryKey().Properties)
            {
                var reader = obj[p.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName]?.CreateReader();
                if (reader!=null)
                {
                    p.PropertyInfo.SetValue(element, serializer.Deserialize(reader, p.PropertyInfo.PropertyType));
                    hasKey=true;
                }
            }

            foreach (var p in entry.Properties.Where(p => p.Metadata.IsConcurrencyToken))
            {
                var reader = obj[p.Metadata.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName]?.CreateReader();
                if (reader!=null)
                {
                    p.Metadata.PropertyInfo.SetValue(element, serializer.Deserialize(obj[p.Metadata.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName].CreateReader(), p.Metadata.PropertyInfo.PropertyType));

                }
            }

            if (entry.State == EntityState.Detached)
            {
                if (hasKey)
                {
                    entry=Context.Attach(element);
                }
                else
                {
                    entry=Context.Add(element);

                }
            }

            return entry;
        }

        private void TraverseDeleteCollections(JToken record, Lazy<Type> clrType)
        {
            foreach (var prop in record.OfType<JProperty>())
            {
                if (prop.Name.EndsWith("@deleted"))
                {
                    var collection = clrType.Value.GetProperties().FirstOrDefault(propertyInfo =>
                       propertyInfo.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == prop.Name.Substring(0, prop.Name.IndexOf('@')));

                    var deletedItems = prop.Value;

                    foreach (var id in deletedItems)
                    {

                        var related = Activator.CreateInstance(collection.PropertyType.GenericTypeArguments[0]);

                        collection.PropertyType.GenericTypeArguments[0].GetProperty("Id").SetValue(related, id.ToObject<Guid>());

                        Context.Attach(related);
                        Context.Remove(related);

                    }

                }
                else if (prop.Value.Type == JTokenType.Array)
                {
                    foreach (var value in prop.Value)
                    {

                        TraverseDeleteCollections(value, new Lazy<Type>(() =>
                        {

                            var collection = clrType.Value.GetProperties().FirstOrDefault(propertyInfo =>
                                propertyInfo.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == prop.Name);

                            return collection.PropertyType.GenericTypeArguments[0];


                        }));
                    }
                }





            }

            //foreach (var collection in entity.Collections)
            //{
            //    var attr = collection.Metadata.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
            //    var deletedItems = record[$"{attr.PropertyName}@deleted"];
            //    if (deletedItems != null)
            //    {
            //        foreach (var id in deletedItems)
            //        {

            //            var related = Activator.CreateInstance(collection.Metadata.TargetEntityType.ClrType);

            //            collection.Metadata.TargetEntityType.ClrType.GetProperty("Id").SetValue(related, id.ToObject<Guid>());



            //            Context.Attach(related);
            //            Context.Remove(related);



            //        }
            //    }



            //}
        }

        private void TraverseDeleteCollections(JToken record, EntityEntry entity)
        {


            foreach (var collection in entity.Collections)
            {
                var attr = collection.Metadata.PropertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                var deletedItems = record[$"{attr.PropertyName}@deleted"];
                if (deletedItems != null)
                {
                    foreach (var id in deletedItems)
                    {

                        var related = Activator.CreateInstance(collection.Metadata.TargetEntityType.ClrType);

                        collection.Metadata.TargetEntityType.ClrType.GetProperty("Id").SetValue(related, id.ToObject<Guid>());



                        Context.Attach(related);
                        Context.Remove(related);



                    }
                }



            }
        }

        public override async ValueTask<EntityEntry> FindAsync(string entityName, params object[] keys)
        {
            var obj = await this.Context.FindAsync(entityName, keys);
            if (obj == null)
                return null;

            return this.Context.Entry(obj);
        }

        public async ValueTask<EntityEntry> FindAsync(EntityRef entityRef)
        {
            var obj = await this.Context.FindAsync(GetCollectionName(entityRef.Type), entityRef.Id);
            if (obj == null)
                return null;

            return this.Context.Entry(obj);
        }

        public string GetCollectionName(string entityName)
        {
            return this.Context.GetCollectionName(entityName);
        }

        public override EntityEntry Add(string entityName, JToken record)
        {
            return this.Context.Add(entityName, record);
        }

        public async ValueTask<EntityEntry> DeleteAsync(string entityName, params object[] keys)
        {
            var record = await this.Context.FindAsync(entityName, keys);
            if (record == null)
                return null;          
            var entry = this.Context.Entry(record);
            entry.State = EntityState.Deleted;
            return entry;

        }
        public DbSet<T> Set<T>() where T : DynamicEntity
        {
            return this.Context.Set<T>();
        }
        public DbSet<TBase> Set<TBase>(Type type) where TBase : DynamicEntity
        {
           
            var a=this.GetType().GetMethod(nameof(Set), 1, new Type[0]).MakeGenericMethod(type).Invoke(this, new object[0]);
            var b = (DbSet<TBase>)a;
            return b;
        }
        public IQueryable<TBase> FromSqlRaw<TBase>(Type type,string sql, params object[] parameters) where TBase : DynamicEntity
        {
            var m = typeof(RelationalQueryableExtensions).GetMethod(nameof(RelationalQueryableExtensions.FromSqlRaw), BindingFlags.Public | BindingFlags.Static);

            var set= this.GetType().GetMethod(nameof(Set), 1, new Type[0]).MakeGenericMethod(type).Invoke(this, new object[0]); ;

            var b=m.MakeGenericMethod(type).Invoke(null, new object[] { set,sql, parameters });

            var c = (IQueryable<TBase>)b;
            return c;


        }



        public IQueryable Set(string name)
        {
            return this.Context.Set(this.Context.GetEntityType(name));
        }
        public IQueryable<TInterface> FilteredSet<TEntity,TInterface>( Expression<Func<TInterface,bool>> filter)
            where TEntity : DynamicEntity 
        {
            //this.Context.GetEntityType(name)
            return this.Context.Set<TEntity>().OfType<TInterface>().Where(filter);
                 
        }
        public IQueryable<TInterface> FilteredSet<TInterface>(string name, Expression<Func<TInterface, bool>> filter)
            
        {
            var type = this.Context.GetEntityType(name);
            //            var method = this.GetType().GetMethod(nameof(FilteredSet), 2, new[] { typeof(Expression<Func<TInterface, bool>>) });
            var method = this.GetType().GetMethods().Where(n => n.Name == nameof(FilteredSet) && n.IsGenericMethod && n.GetGenericArguments().Length == 2).FirstOrDefault();

            return method.MakeGenericMethod(type, typeof(TInterface)).Invoke(this, new object[] { filter }) as IQueryable<TInterface>;


        }

        public IQueryable<T> Set<T>(string name)
        {

            return this.Context.Set(this.Context.GetEntityType(name)).Cast<T>();
        }
    }

    public static class ExpressionExtensions
    {
        // Given an expression for a method that takes in a single parameter (and
        // returns a bool), this method converts the parameter type of the parameter
        // from TSource to TTarget.
        public static Expression<Func<TTarget, bool>> Convert<TSource, TTarget>(
          this Expression<Func<TSource, bool>> root)
        {
            var visitor = new ParameterTypeVisitor<TSource, TTarget>();
            return (Expression<Func<TTarget, bool>>)visitor.Visit(root);
        }

        class ParameterTypeVisitor<TSource, TTarget> : ExpressionVisitor
        {
            private ReadOnlyCollection<ParameterExpression> _parameters;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return _parameters?.FirstOrDefault(p => p.Name == node.Name)
                  ?? (node.Type == typeof(TSource) ? Expression.Parameter(typeof(TTarget), node.Name) : node);
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                _parameters = VisitAndConvert<ParameterExpression>(node.Parameters, "VisitLambda");
                return Expression.Lambda(Visit(node.Body), _parameters);
            }
        }
    }
}
