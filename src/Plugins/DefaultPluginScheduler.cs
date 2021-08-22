using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public class PipelineExecutionStage
    {

    }
    public class DefaultPluginScheduler : IPluginScheduler
    {
        public Task ScheduleAsync(EntityPlugin plugin, object entity)
        {
            return Task.CompletedTask;
        }
    }

    public class SavingIncepter : SaveChangesInterceptor
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private readonly IEnumerable<EntityPlugin> plugins;

        public SavingIncepter(IServiceScopeFactory serviceScopeFactory, IEnumerable<EntityPlugin> plugins)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new System.ArgumentNullException(nameof(serviceScopeFactory));
            this.plugins = plugins ?? throw new System.ArgumentNullException(nameof(plugins));
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

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var stage= eventData.Context.GetService<PipelineExecutionStage>();
            var plugins = eventData.Context.GetService<IEnumerable<EntityPlugin>>();
            var http = eventData.Context.GetService<IHttpContextAccessor>();
            var context = http.HttpContext;
            foreach (var entity  in eventData.Context.ChangeTracker.Entries().Where(o=>o.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged))
            {
                var operation = GetOperation(entity.State);
               
                foreach (var plugin in plugins.Where(plugin => plugin.Operation == operation && plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type.IsAssignableFrom(entity.Entity.GetType())))
                {
                    var ctx = await plugin.Execute(eventData.Context.GetService<IServiceProvider>(), context.User, entity);
                  // errors.AddRange(ctx.Errors);
                }
            }





            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
