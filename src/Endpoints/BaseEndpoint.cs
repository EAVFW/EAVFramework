using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Validation;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class Tracker : HashSet<object>
    {

    }
    internal class BaseEndpoint
    {
        protected readonly IEnumerable<EntityPlugin> _plugins;
        private readonly EntityPluginOperation _operation;
        protected readonly IPluginScheduler _pluginScheduler;

        public BaseEndpoint(IEnumerable<EntityPlugin> plugins, EntityPluginOperation operation, IPluginScheduler pluginScheduler)
        {
            _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
            _operation = operation;
            _pluginScheduler = pluginScheduler ?? throw new ArgumentNullException(nameof(pluginScheduler));
        }

        protected async Task RunAsyncPostOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
          

           
        }

        protected async Task RunPipelineAsync<TContext>(IServiceProvider serviceProvider, HttpContext context, OperationContext<TContext> operation) where TContext : DynamicContext
        {

            var trans = await operation.Context.Database.BeginTransactionAsync();

            await RunPreOperation(_operation, serviceProvider, context, operation.Entity);

            await operation.Context.SaveChangesAsync();

            await RunPostOperation(_operation,serviceProvider, context, operation.Entity);

            await operation.Context.SaveChangesAsync();

            await trans.CommitAsync();

        }

        protected async Task RunPostOperation(EntityPluginOperation operation, IServiceProvider serviceProvider, HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            var tracker = (context.Items.ContainsKey("PostOperationEAVTracker") ? context.Items["PostOperationEAVTracker"] : context.Items["PostOperationEAVTracker"] = new Tracker()) as Tracker;
            tracker.Add(a.Entity);

            // var opeation = GetOperation(a.State) ;



            foreach (var plugin in _plugins.Where(plugin => plugin.Operation == operation && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(a.Entity.GetType())))
            {
                await plugin.Execute(serviceProvider, context.User, a);
            }


            foreach (var navigation in a.References.Where(t => t.TargetEntry != null && t.IsModified))
            {
                if (!tracker.Contains(navigation.TargetEntry.Entity) && navigation.TargetEntry.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                    await RunPostOperation(GetOperation(navigation.TargetEntry.State), serviceProvider, context, navigation.TargetEntry);
            }


            foreach (var collection in a.Collections ?? EmptyCollection)
            {
                if (collection.CurrentValue != null)
                {
                    foreach (var item in collection.CurrentValue)
                    {
                        var entry = collection.FindEntry(item);
                        if (!tracker.Contains(entry.Entity) && entry.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                            await RunPostOperation(GetOperation(entry.State), serviceProvider, context, entry);

                    }
                    //foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type.IsAssignableFrom(collection.Metadata.TargetEntityType.ClrType)))
                    //{
                    //    await plugin.Execute(serviceProvider, context.User, collection);
                    //}
                }
            }
        }

        private EntityPluginOperation GetOperation(Microsoft.EntityFrameworkCore.EntityState state)
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

        protected async Task RunPreOperation(EntityPluginOperation operation,IServiceProvider serviceProvider, HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            var tracker = (context.Items.ContainsKey("PreOperationEAVTracker") ? context.Items["PreOperationEAVTracker"] : context.Items["PreOperationEAVTracker"] = new Tracker()) as Tracker;
            tracker.Add(a.Entity);


            foreach (var plugin in _plugins.Where(plugin => plugin.Operation == operation && plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type.IsAssignableFrom(a.Entity.GetType())))
            {
                await plugin.Execute(serviceProvider, context.User, a);
            }


            foreach (var navigation in a.References.Where(t => t.TargetEntry != null && t.IsModified))
            {
                if (!tracker.Contains(navigation.TargetEntry.Entity) && navigation.TargetEntry.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                    await RunPreOperation(GetOperation(navigation.TargetEntry.State),serviceProvider, context, navigation.TargetEntry);
            }

            foreach (var collection in a.Collections ?? EmptyCollection)
            {
                if (collection.CurrentValue != null)
                {
                    foreach (var item in collection.CurrentValue)
                    {
                        var entry = collection.FindEntry(item);
                        if (!tracker.Contains(entry.Entity) && entry.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                            await RunPreOperation(GetOperation(entry.State),serviceProvider, context, entry);

                    }

                    //foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type.IsAssignableFrom(collection.Metadata.TargetEntityType.ClrType)))
                    //{
                    //    await plugin.Execute(serviceProvider, context.User, collection);
                    //}
                }
            }
        }
        static IEnumerable<CollectionEntry> EmptyCollection = Enumerable.Empty<CollectionEntry>();
        protected async Task<List<ValidationError>> RunPreValidation(IServiceProvider serviceProvider, HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a, EntityPluginOperation? operation=null)
        {
            var tracker = (context.Items.ContainsKey("PreValidationEAVTracker") ? context.Items["PreValidationEAVTracker"] : context.Items["PreValidationEAVTracker"] = new Tracker()) as Tracker;
            tracker.Add(a.Entity);

            var errors = new List<ValidationError>();

            operation ??= _operation;

            foreach (var plugin in _plugins.Where(plugin => plugin.Operation == operation && plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type.IsAssignableFrom(a.Entity.GetType())))
            {
                var ctx = await plugin.Execute(serviceProvider, context.User, a);
                errors.AddRange(ctx.Errors);
            }


            foreach (var navigation in a.References.Where(t => t.TargetEntry != null && (t.IsModified || t.TargetEntry.State == Microsoft.EntityFrameworkCore.EntityState.Added)))
            {
                if (!tracker.Contains(navigation.TargetEntry.Entity) && navigation.TargetEntry.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                    errors.AddRange(await RunPreValidation(serviceProvider, context, navigation.TargetEntry, GetOperation(navigation.TargetEntry.State)));
            }

            foreach (var collection in a.Collections ?? EmptyCollection)
            {
                if (collection.CurrentValue != null)
                {
                    foreach (var item in collection.CurrentValue)
                    {
                        var entry = collection.FindEntry(item);
                        if (!tracker.Contains(entry.Entity) && entry.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                            errors.AddRange(await RunPreValidation(serviceProvider, context, entry, GetOperation(entry.State)));

                    }

                    //foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type.IsAssignableFrom(collection.Metadata.TargetEntityType.ClrType)))
                    //{
                    //    await plugin.Execute(serviceProvider, context.User, collection);
                    //}
                }
            }

            return errors;
        }

    }
}
