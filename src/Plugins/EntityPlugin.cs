using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
  
    public abstract class EntityPlugin
    {
        public EntityPluginMode Mode { get; set; } = EntityPluginMode.Sync;

        public EntityPluginExecution Execution { get; set; }
        public EntityPluginOperation Operation { get;  set; }
        public int Order { get; set; }

        public Type Type { get; set; }
        public Type Handler { get; set; }

        public abstract Task<PluginContext> Execute(IServiceProvider services, EntityEntry entity);
    }
}
