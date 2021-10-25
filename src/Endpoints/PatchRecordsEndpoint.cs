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
using System.Reflection;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
    public class EAVResource
    {
        public Type EntityType { get; set; }
        public string EntityCollectionSchemaName { get;  set; }
    }
    public struct ReadOptions
    {
        public bool LogPayload { get; set; }
        public string RecordId { get; set; }
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

            var entity = await _context.PatchAsync(entityName, Guid.Parse(recordId), record);
             
            var _operation = await _context.SaveChangesAsync(context.User);


            if (_operation.Errors.Any())
                return new DataValidationErrorResult(new { errors = _operation.Errors });

            return new DataEndpointResult(new { id = entity.CurrentValues.GetValue<Guid>("Id") });



        }

      
    }
}
