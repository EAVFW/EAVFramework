using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    internal abstract class EntityPlugin
    {
        public EntityPluginExecution Execution { get; set; }
        public EntityPluginOperation Operation { get;  set; }
        public int Order { get; set; }

        public Type Type { get; set; }
        public Type Handler { get; set; }

        public abstract Task<PluginContext> Execute(IServiceProvider services, EntityEntry entity);
    }
}
