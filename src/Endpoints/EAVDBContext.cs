using DotNetDevOps.Extensions.EAVFramework.Plugins;
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
using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class DataUrlHelper
    {
        public string Name { get; set; }

        public string ContentType { get; set; }

        public byte[] Data { get; set; }

        public void Parse(string dataUrl)
        {
            if(!dataUrl.StartsWith("data:"))
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
    public class EAVDBContext<TContext> where TContext : DynamicContext
    {
        private readonly TContext context;
        private readonly PluginsAccesser plugins;
        private readonly ILogger<EAVDBContext<TContext>> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IPluginScheduler<TContext> pluginScheduler;

     //   private static JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {  Converters = { new DataUrlConverter } });
      

        public EAVDBContext(TContext context, PluginsAccesser plugins, ILogger<EAVDBContext<TContext>> logger, IServiceProvider serviceProvider, IPluginScheduler<TContext> pluginScheduler)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.plugins = plugins;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.pluginScheduler = pluginScheduler;
            context.EnsureModelCreated();
        }
        public async Task MigrateAsync()
        {
            var migrator = context.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            await migrator.MigrateAsync();

        }
        public async ValueTask<JToken> ReadRecordAsync(HttpContext context ,ReadOptions options)
        {
            if (options.LogPayload)
            {
                var reader = new StreamReader(context.Request.BodyReader.AsStream());
                var text = await reader.ReadToEndAsync();
                logger.LogInformation("Reading Payload : {Payload}", text);

                var record = JToken.Parse(text);
                if(!string.IsNullOrEmpty(options.RecordId))
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

        internal EAVResource CreateEAVResource(string entityName)
        {
            var type = context.GetEntityType(entityName);
            return new EAVResource
            {
                EntityType = type,
                EntityCollectionSchemaName =  type.GetCustomAttribute<EntityAttribute>().CollectionSchemaName
            };
        }

        public ValueTask<OperationContext<TContext>> SaveChangesAsync(ClaimsPrincipal user, Func<Task> onBeforeCommit = null)
        {
            return this.context.SaveChangesPipeline(serviceProvider, user, plugins, pluginScheduler, onBeforeCommit);
        }

        public async ValueTask<EntityEntry> PatchAsync(string entityName, Guid recordId, JToken record)
        {
            var entity = await FindAsync(entityName, recordId);

            var relatedProps = record.OfType<JProperty>().Where(p => p.Value.Type == JTokenType.Object).ToArray();

            await Task.WhenAll(relatedProps.Select(related =>
                entity.References.First(c => string.Equals(c.Metadata.Name, related.Name, StringComparison.OrdinalIgnoreCase)).LoadAsync()));
           
            var serializer = new JsonSerializer();

            serializer.Populate(record.CreateReader(), entity.Entity);
            entity.State = EntityState.Modified;

           


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



                        context.Attach(related);
                        context.Remove(related);
                    }
                }

            }
            return entity;
        }

        public async ValueTask<EntityEntry> FindAsync(string entityName, params object[] keys)
        {
            var obj= await this.context.FindAsync(entityName, keys);
            if (obj == null)
                return null;

            return this.context.Entry(obj);
        }

        public EntityEntry Add(string entityName, JToken record)
        {
            return this.context.Add(entityName, record);
        }

        public async ValueTask<EntityEntry> DeleteAsync(string entityName, params object[] keys)
        {
            var record=await this.context.FindAsync(entityName, keys);
            if (record == null)
                return null;
            var entry= this.context.Entry(record);
            entry.State = EntityState.Deleted;
            return entry;

        }
        public DbSet<T> Set<T>() where T: DynamicEntity
        {
            return this.context.Set<T>();
        }
    }
}
