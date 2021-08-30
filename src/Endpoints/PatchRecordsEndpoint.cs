using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
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
            this TContext dynamicContext,string entityName,JToken record, HttpContext context, IEnumerable<EntityPlugin> plugins,
            IPluginScheduler pluginScheduler)
             where TContext : DynamicContext
        {
            var strategy = dynamicContext.Database.CreateExecutionStrategy();

            var _operation = await strategy.ExecuteAsync(async () =>
            {
                using var scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

                var operation = new OperationContext<TContext>
                {
                    Context = dynamicContext
                };

                operation.Entity = dynamicContext.Update(entityName, record);

                var errors = new List<ValidationError>();
                var trackedEntities = dynamicContext.ChangeTracker.Entries()
                    .Where(e => e.State != EntityState.Unchanged)
                    .Select(e => new { operation = GetOperation(e.State), entity = e })
                    .ToArray();

                foreach (var entity in trackedEntities)
                {


                    //  operation.Errors = await RunPreValidation(scope.ServiceProvider, context, operation.Entity);
                    foreach (var plugin in plugins.Where(plugin => plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(scope.ServiceProvider, context.User, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }
                operation.Errors = errors;

                if (operation.Errors.Any())
                    return operation;


                var trans = await operation.Context.Database.BeginTransactionAsync();

                foreach (var entity in trackedEntities)
                {


                    //  operation.Errors = await RunPreValidation(scope.ServiceProvider, context, operation.Entity);
                    foreach (var plugin in plugins.Where(plugin => plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(scope.ServiceProvider, context.User, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }

                await dynamicContext.SaveChangesAsync();



                foreach (var entity in trackedEntities)
                {


                    //  operation.Errors = await RunPreValidation(scope.ServiceProvider, context, operation.Entity);
                    foreach (var plugin in plugins.Where(plugin => plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(scope.ServiceProvider, context.User, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }

                await operation.Context.SaveChangesAsync();

                await trans.CommitAsync();

                foreach (var entity in trackedEntities)
                {

                    foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Async && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        await pluginScheduler.ScheduleAsync(plugin, entity.entity.Entity);
                        // await plugin.Execute(context.RequestServices, a);
                    }
                }

                return operation;
            });

            return _operation;

        }
    }
    internal class PatchRecordsEndpoint<TContext> : BaseEndpoint, IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly TContext _context;

        public PatchRecordsEndpoint(TContext context, IEnumerable<EntityPlugin> plugins, IPluginScheduler pluginScheduler) :base(plugins, EntityPluginOperation.Update, pluginScheduler)
        {
            _context = context;
        }

       
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;

            var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream())));

           

           var _operation= await _context.SaveChangesPipeline(entityName, record,context, _plugins,_pluginScheduler);
             
            return new DataEndpointResult(new { id = _operation.Entity.CurrentValues.GetValue<Guid>("Id") });


        }
    }
}
