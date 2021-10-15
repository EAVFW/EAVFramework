using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Validation;
using static DotNetDevOps.Extensions.EAVFramework.Constants;
using System.Security.Claims;
using System.Collections;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class PluginsAccesser : IEnumerable<EntityPlugin>
    {
        IEnumerable<EntityPlugin> _plugins { get; }
        public PluginsAccesser(IEnumerable<EntityPlugin> plugins)
        {
            _plugins = plugins.OrderBy(x => x.Order).ToArray();
        }

        public IEnumerator<EntityPlugin> GetEnumerator()
        {
            return _plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _plugins.GetEnumerator();
        }
    }

    public struct ReadOptions
    {
        public bool LogPayload { get; set; }
        public string RecordId { get; set; }
    }
    public class EAVDBContext<TContext> where TContext : DynamicContext
    {
        private readonly TContext context;
        private readonly PluginsAccesser plugins;
        private readonly ILogger<EAVDBContext<TContext>> logger;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IPluginScheduler pluginScheduler;

      

        public EAVDBContext(TContext context, PluginsAccesser plugins, ILogger<EAVDBContext<TContext>> logger, IServiceScopeFactory scopeFactory, IPluginScheduler pluginScheduler)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.plugins = plugins;
            this.logger = logger;
            this.scopeFactory = scopeFactory;
            this.pluginScheduler = pluginScheduler;
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


        public ValueTask<OperationContext<TContext>> SaveChangesAsync(ClaimsPrincipal user)
        {
            return this.context.SaveChangesPipeline(scopeFactory, user, plugins, pluginScheduler);
        }

        public EntityEntry Update(string entityName, JToken data)
        {
            return this.context.Update(entityName, data);
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
    public static class ContextExtensions
    {
        private static EntityPluginOperation GetOperation(Microsoft.EntityFrameworkCore.EntityState state)
        {
            switch (state)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    return EntityPluginOperation.Create;
                case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                    return EntityPluginOperation.Delete;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    return EntityPluginOperation.Update;
            }
            throw new InvalidOperationException("Unknown Operation");
        }

        public static async ValueTask<OperationContext<TContext>> SaveChangesPipeline<TContext>(
           this TContext dynamicContext, IServiceScopeFactory scopeFactory, ClaimsPrincipal user, IEnumerable<EntityPlugin> plugins,
           IPluginScheduler pluginScheduler)
            where TContext : DynamicContext
        {
            var strategy = dynamicContext.Database.CreateExecutionStrategy();

            var _operation = await strategy.ExecuteAsync(async () =>
            {
                using var scope = scopeFactory.CreateScope();

                var _operation = new OperationContext<TContext>
                {
                    Context = dynamicContext
                };


                var errors = new List<ValidationError>();
                var trackedEntities = dynamicContext.ChangeTracker.Entries()
                    .Where(e => e.State != EntityState.Unchanged)
                    .Select(e => new { operation = GetOperation(e.State), entity = e })
                    .ToArray();

                foreach (var entity in trackedEntities)
                {
                    foreach (var plugin in plugins.Where(plugin => plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(scope.ServiceProvider, user, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }
                _operation.Errors = errors;

                if (_operation.Errors.Any())
                    return _operation;


                var trans = await _operation.Context.Database.BeginTransactionAsync();

                foreach (var entity in trackedEntities)
                {
                    foreach (var plugin in plugins.Where(plugin => plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(scope.ServiceProvider, user, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }

                await dynamicContext.SaveChangesAsync();

                foreach (var entity in trackedEntities)
                {


                    //  operation.Errors = await RunPreValidation(scope.ServiceProvider, context, operation.Entity);
                    foreach (var plugin in plugins.Where(plugin => plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(scope.ServiceProvider, user, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }

                await _operation.Context.SaveChangesAsync();

                await trans.CommitAsync();

                foreach (var entity in trackedEntities)
                {

                    foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Async && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        await pluginScheduler.ScheduleAsync(plugin, entity.entity.Entity);
                    }
                }

                return _operation;
            });

            return _operation;

        }


    }

    internal class PatchRecordsEndpoint<TContext> : IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly EAVDBContext<TContext> _context;
        private readonly ILogger<PatchRecordsEndpoint<TContext>> logger;
        private readonly IConfiguration configuration;

        public PatchRecordsEndpoint(
            EAVDBContext<TContext> context,
            ILogger<PatchRecordsEndpoint<TContext>> logger,
            IConfiguration configuration

            )
        {
            _context = context;
            this.logger = logger;
            this.configuration = configuration;
        }


        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
             
            JToken record = await _context.ReadRecordAsync(context,new ReadOptions {RecordId= recordId, LogPayload = configuration.GetValue<bool>($"EAVFramework:PatchRecordsEndpoint:LogPayload", false) });
            var entity = await _context.FindAsync(entityName, Guid.Parse( recordId));

            var serializer = new JsonSerializer();
             
            serializer.Populate(record.CreateReader(), entity.Entity);
            entity.State = EntityState.Modified;

           // var entity = _context.Update(entityName, record); ;

            var _operation = await _context.SaveChangesAsync(context.User);


            if (_operation.Errors.Any())
                return new DataValidationErrorResult(new { errors = _operation.Errors });

            return new DataEndpointResult(new { id = entity.CurrentValues.GetValue<Guid>("Id") });



        }

      
    }
}
