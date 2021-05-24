using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    internal class EntityPlugin<TContext,T> : EntityPlugin
        where TContext : DynamicContext
        where T : DynamicEntity
    {
       
     

        public static ConcurrentDictionary<Type, MethodInfo> Invokers = new ConcurrentDictionary<Type, MethodInfo>();

       

        public override async Task<PluginContext> Execute(IServiceProvider services, EntityEntry entity) 
        {
            var plugincontext = new PluginContext<TContext, T>
            {
                Input = entity.Entity as T,
                DB= entity.Context as TContext

            };
            //var pluginContext = Activator.CreateInstance(typeof(PluginContext<,>).MakeGenericType(typeof(DBContext), entity.Entity.GetType()));

            var handler = services.GetService(Handler) as IPlugin<TContext, T>;
            await handler.Execute(plugincontext);
            //var invoker = Invokers.GetOrAdd(entityType, (t) => typeof(IPlugin<,>).MakeGenericType(t).GetMethod("Execute"));

            //var task = invoker.Invoke(handler, new object[] { pluginContext }) as Task;
            //await task;

            return plugincontext;
        }
    }
}
