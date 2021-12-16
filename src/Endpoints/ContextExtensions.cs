using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Validation;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public struct TrackedPipelineItem
    {
        public EntityPluginOperation Operation { get; set; }
        public EntityEntry Entity { get; set; }
    }
    public static class ContextExtensions
    {
        private static async ValueTask<bool> EmptyPreValQueue<TContext>(IServiceProvider serviceProvider,  TContext dynamicContext, OperationContext<TContext> operationContext, IEnumerable<EntityPlugin> plugins, ClaimsPrincipal user, EntityPluginExecution stage, HashSet<object> discovered_prevalitems)
       where TContext : DynamicContext
        {
           

            var items = dynamicContext.ChangeTracker.Entries()
           .Where(e => e.State != EntityState.Unchanged)           
           .ToArray();

            var queue_for_preval = new Queue<TrackedPipelineItem>();

            foreach (var item in items)
            {
                if (!discovered_prevalitems.Contains(item.Entity))
                {
                    discovered_prevalitems.Add(item.Entity);
                    queue_for_preval.Enqueue(new TrackedPipelineItem { Operation = GetOperation(item.State), Entity = item });
                }
            }

            if (!queue_for_preval.Any())
            {
                return false;
            }

            while (queue_for_preval.Count >0)
            {
                var entity = queue_for_preval.Dequeue();
                foreach (var plugin in plugins.Where(plugin => plugin.Mode == EntityPluginMode.Sync && plugin.Operation == entity.Operation && plugin.Execution == stage && plugin.Type.IsAssignableFrom(entity.Entity.Entity.GetType())))
                {
                    var ctx = await plugin.Execute(serviceProvider, user, entity.Entity);
                    operationContext.Errors.AddRange(ctx.Errors);
                }

                //foreach (var tracked in dynamicContext.ChangeTracker.Entries()
                //    .Where(e => e.State != EntityState.Unchanged).ToArray())
                //{
                //    if (!discovered_prevalitems.Contains(tracked.Entity))
                //    {
                //        discovered_prevalitems.Add(tracked.Entity);
                //        queue_for_preval.Enqueue(new TrackedPipelineItem { Operation = GetOperation(tracked.State), Entity = tracked });
                //    }

                //}

            }

            return true;
        }

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


              

                
                var discovered_prevalitems = new HashSet<object>();

                var newItems_preval = true;
                do
                {
                    newItems_preval=   await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);
                } while (newItems_preval);


                if (_operation.Errors.Any())
                    return _operation;



                var trans = await _operation.Context.Database.BeginTransactionAsync();

                var discovered_preoperation = new HashSet<object>();

                var newItems_preop = true;
                do
                {
                    newItems_preop= await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreOperation, discovered_preoperation);

                    await dynamicContext.SaveChangesAsync();

                    newItems_preval= await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);

                } while (newItems_preop || newItems_preval);
               
                 


               

                var discovered_postoperation = new HashSet<object>();
                var newItems_postop = true;

                do
                {
                    newItems_postop = await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PostOperation, discovered_postoperation);

                    await _operation.Context.SaveChangesAsync();

                    newItems_preval= await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);
                     
                    newItems_preop = await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreOperation, discovered_preoperation);

                    await _operation.Context.SaveChangesAsync();

                } while (newItems_preop || newItems_preval || newItems_postop);
                 
                
              
                if (onBeforeCommit != null)
                    await onBeforeCommit(_operation);

                
                if (_operation.Errors.Any())
                    return _operation;

                //await onBeforeCommit?.Invoke() ?? Task.CompletedTask;

                await trans.CommitAsync();


                var items = dynamicContext.ChangeTracker.Entries()
               .Where(e => e.State != EntityState.Unchanged)
               .ToArray();


                foreach (var item in items)
                {
                    var entity = new TrackedPipelineItem { Operation = GetOperation(item.State), Entity = item };
                    foreach (var plugin in plugins.Where(plugin =>
                        plugin.Mode == EntityPluginMode.Async &&
                        plugin.Execution == EntityPluginExecution.PostOperation &&
                        plugin.Operation == entity.Operation &&
                        plugin.Type.IsAssignableFrom(entity.Entity.Entity.GetType())))
                    {
                        await pluginScheduler.ScheduleAsync(plugin,user.FindFirstValue("sub"), entity.Entity.Entity);
                    }
                }

                return _operation;
            });

            return _operation;

        }


    }
}
