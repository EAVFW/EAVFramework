using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Validation;
using System.Security.Claims;

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
           this TContext dynamicContext, IServiceProvider serviceProvider, ClaimsPrincipal user, IEnumerable<EntityPlugin> plugins,
           IPluginScheduler<TContext> pluginScheduler, Func<OperationContext<TContext>,Task> onBeforeCommit = null)
            where TContext : DynamicContext
        {
            var strategy = dynamicContext.Database.CreateExecutionStrategy();

            var _operation = await strategy.ExecuteAsync(async () =>
            {
              //  using var scope = scopeFactory.CreateScope();

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
                    foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Sync && plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(serviceProvider, user, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }
                _operation.Errors = errors;

                if (_operation.Errors.Any())
                    return _operation;


                var trans = await _operation.Context.Database.BeginTransactionAsync();

                foreach (var entity in trackedEntities)
                {
                    foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Sync && plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(serviceProvider, user, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }

                await dynamicContext.SaveChangesAsync();

                foreach (var entity in trackedEntities)
                {


                    //  operation.Errors = await RunPreValidation(scope.ServiceProvider, context, operation.Entity);
                    foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Sync && plugin.Operation == entity.operation && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        var ctx = await plugin.Execute(serviceProvider, user, entity.entity);
                        errors.AddRange(ctx.Errors);
                    }
                }

                await _operation.Context.SaveChangesAsync();
              
                if (onBeforeCommit != null)
                    await onBeforeCommit(_operation);

                
                if (_operation.Errors.Any())
                    return _operation;

                //await onBeforeCommit?.Invoke() ?? Task.CompletedTask;

                await trans.CommitAsync();

                foreach (var entity in trackedEntities)
                {

                    foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Async && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(entity.entity.Entity.GetType())))
                    {
                        await pluginScheduler.ScheduleAsync(plugin,user.FindFirstValue("sub"), entity.entity.Entity);
                    }
                }

                return _operation;
            });

            return _operation;

        }


    }
}
