using EAVFramework.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EAVFramework.Validation;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.CodeAnalysis;

namespace EAVFramework.Endpoints
{
    public struct TrackedPipelineItem : IEquatable<TrackedPipelineItem>
    {
        public EntityPluginOperation Operation { get; set; }
        public EntityEntry Entity { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entity?.Entity?.GetHashCode()??-1, Operation.GetHashCode());
        }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var objectToCompareWith = (TrackedPipelineItem)obj;

            return objectToCompareWith.GetHashCode() == this.GetHashCode();

        }

        public bool Equals([AllowNull] TrackedPipelineItem other)
        {
            return Equals(other, this);
        }
    }
    public static class ContextExtensions
    {
        private static async ValueTask<bool> EmptyPreValQueue<TContext>(IServiceProvider serviceProvider,  TContext dynamicContext, OperationContext<TContext> operationContext, IEnumerable<EntityPlugin> plugins, ClaimsPrincipal user, EntityPluginExecution stage, HashSet<TrackedPipelineItem> discovered_prevalitems, HashSet<TrackedPipelineItem> preoperation_items = null)
       where TContext : DynamicContext
        {
           

            var items = preoperation_items?.ToArray() ?? dynamicContext.ChangeTracker.Entries()
           .Where(e => e.State != EntityState.Unchanged)    
           .Select(item=> new TrackedPipelineItem { Operation = GetOperation(item.State), Entity = item })
           .ToArray();
            

            var queue_for_preval = new Queue<TrackedPipelineItem>();

            foreach (var item in items)
            {
                if (!discovered_prevalitems.Contains(item))
                {
                    discovered_prevalitems.Add(item);
                    queue_for_preval.Enqueue(item);
                }
            }

            if (!queue_for_preval.Any())
            {
                return false;
            }

            while (queue_for_preval.Count >0)
            {
                var entity = queue_for_preval.Dequeue();
                foreach (var plugin in plugins

                    .Where(plugin =>     
                      
                        plugin.Mode == EntityPluginMode.Sync && 
                        plugin.Operation == entity.Operation && 
                        plugin.Execution == stage && plugin.ShouldPluginBeExecued<TContext>(dynamicContext, entity)))
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


              

                
                var discovered_prevalitems = new HashSet<TrackedPipelineItem>();

                var newItems_preval = true;
                do
                {
                    newItems_preval=   await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);
                } while (newItems_preval);


                if (_operation.Errors.Any())
                    return _operation;



                var trans = await _operation.Context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted);

                var discovered_preoperation = new HashSet<TrackedPipelineItem>();

                var newItems_preop = true;
                do
                {
                    newItems_preop= await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreOperation, discovered_preoperation);

                    do
                    {
                        newItems_preval=   await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);
                    } while (newItems_preval);
                     
                } while (newItems_preop || newItems_preval);


                _operation.PreOperationChanges = dynamicContext.ChangeTracker.DebugView.ShortView;
                await dynamicContext.SaveChangesAsync();





                var discovered_postoperation = new HashSet<TrackedPipelineItem>();
                var newItems_postop = true;

                do
                {
                    newItems_postop = await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PostOperation, discovered_postoperation, discovered_preoperation);

                    do
                    {
                        newItems_preval=   await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);
                    } while (newItems_preval);

                    do
                    {
                        newItems_preop= await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreOperation, discovered_preoperation);

                        do
                        {
                            newItems_preval=   await EmptyPreValQueue(serviceProvider, dynamicContext, _operation, plugins, user, EntityPluginExecution.PreValidate, discovered_prevalitems);
                        } while (newItems_preval);

                    } while (newItems_preop || newItems_preval);

 
 
                } while (newItems_preop || newItems_preval || newItems_postop);

                _operation.PostOperationChanges = dynamicContext.ChangeTracker.DebugView.ShortView;
                await _operation.Context.SaveChangesAsync();



                if (onBeforeCommit != null)
                    await onBeforeCommit(_operation);

                
                if (_operation.Errors.Any())
                    return _operation;

                //await onBeforeCommit?.Invoke() ?? Task.CompletedTask;

                await trans.CommitAsync();


         


                foreach (var entity in discovered_prevalitems)
                {
                    
                    foreach (var plugin in plugins.Where(plugin =>
                        plugin.Mode == EntityPluginMode.Async &&
                        plugin.Execution == EntityPluginExecution.PostOperation &&
                        plugin.Operation == entity.Operation && plugin.ShouldPluginBeExecued<TContext>(dynamicContext, entity)))
                    {
                        await pluginScheduler.ScheduleAsync(plugin,user.FindFirstValue("sub"), entity.Entity.Entity);
                    }
                }

                _operation.Context.ChangeTracker.Clear();

                return _operation;
            });

            return _operation;

        }


    }
}
