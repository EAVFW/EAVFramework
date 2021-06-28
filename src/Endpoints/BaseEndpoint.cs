using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    internal class BaseEndpoint
    {
        private readonly IEnumerable<EntityPlugin> _plugins;
        private readonly IPluginScheduler _pluginScheduler;

        public BaseEndpoint(IEnumerable<EntityPlugin> plugins, IPluginScheduler pluginScheduler)
        {
            _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
            _pluginScheduler = pluginScheduler ?? throw new ArgumentNullException(nameof(pluginScheduler));
        }

        protected async Task RunAsyncPostOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            foreach (var plugin in _plugins.Where(plugin => plugin.Mode == EntityPluginMode.Async && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type == a.Entity.GetType()))
            {
                await _pluginScheduler.ScheduleAsync(plugin, a.Entity);
                // await plugin.Execute(context.RequestServices, a);
            }

            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                await RunAsyncPostOperation(context, navigation.TargetEntry);
            }
        }

        protected async Task RunPipelineAsync<TContext>(HttpContext context, OperationContext<TContext> operation) where TContext : DynamicContext
        {
            var trans = await operation.Context.Database.BeginTransactionAsync();

            await RunPreOperation(context, operation.Entity);

            await operation.Context.SaveChangesAsync();

            await RunPostOperation(context, operation.Entity);

            await operation.Context.SaveChangesAsync();

            await trans.CommitAsync();
           
        }

        protected async Task RunPostOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type == a.Entity.GetType()))
            {
                await plugin.Execute(context.RequestServices, a);
            }

            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                await RunPostOperation(context, navigation.TargetEntry);
            }
        }

        protected async Task RunPreOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                await RunPreOperation(context, navigation.TargetEntry);
            }

            foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type == a.Entity.GetType()))
            {
                await plugin.Execute(context.RequestServices, a);
            }
        }

        protected async Task<List<ValidationError>> RunPreValidation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {


            var errors = new List<ValidationError>();

            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                errors.AddRange(await RunPreValidation(context, navigation.TargetEntry));
            }

            foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type == a.Entity.GetType()))
            {
                var ctx = await plugin.Execute(context.RequestServices, a);
                errors.AddRange(ctx.Errors);
            }

            return errors;
        }

    }
}
