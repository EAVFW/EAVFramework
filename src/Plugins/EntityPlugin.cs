using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EAVFramework.Plugins
{

    public class AnyEntity : DynamicEntity
    {

    }

    public abstract class EntityPlugin<TContext> : EntityPlugin
       where TContext : DynamicContext
    {

    }
       
    public abstract class EntityPlugin
         
    {
        public EntityPluginMode Mode { get; set; } = EntityPluginMode.Sync;

        public EntityPluginExecution Execution { get; set; }
        public EntityPluginOperation Operation { get;  set; }
        public int Order { get; set; }

        public Type Type { get; set; }
        public Type Handler { get; set; }

        public abstract Task<PluginContext> Execute(IServiceProvider services, ClaimsPrincipal principal, EntityEntry entity);
        public abstract Task Execute(IServiceProvider services, ClaimsPrincipal principal, CollectionEntry entity);
    }
}
