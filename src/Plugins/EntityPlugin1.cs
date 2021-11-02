using DotNetDevOps.Extensions.EAVFramework.Configuration;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{

    public interface IPluginRegistration
    {

    }

    public static class PluginAutoReg
    {
        public static IEAVFrameworkBuilder AddPlugin<T>(this IEAVFrameworkBuilder builder)
            where T : class, IPluginRegistration
        {

            builder.Services.AddTransient<T>();
            var pluginType = typeof(T);

            foreach (var attr in pluginType.GetCustomAttributes<PluginRegistrationAttribute>())
            {

                var plugin = pluginType.GetInterface("IPlugin`2").GenericTypeArguments;

                var contexttype = plugin[0];
                var entitytype = plugin[1];

                var entry = (EntityPlugin)Activator.CreateInstance(typeof(EntityPlugin<,>).MakeGenericType(contexttype, entitytype));

                entry.Execution = attr.Execution;
                entry.Mode = attr.Mode;
                entry.Order = attr.Order;
                entry.Operation = attr.Operation;
                entry.Type = entitytype;
                entry.Handler = pluginType;


                builder.Services.AddSingleton(entry);
            }

            return builder;
        }
       
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple =true, Inherited =false)]
    public class PluginRegistrationAttribute : Attribute
    {
        public EntityPluginExecution Execution { get; }
        public EntityPluginOperation Operation { get; }
        public EntityPluginMode Mode { get; }
        public int Order { get; }
        public PluginRegistrationAttribute(EntityPluginExecution execution, EntityPluginOperation operation, int order = 0, EntityPluginMode mode = EntityPluginMode.Sync)
        {
            Execution = execution;
            Operation = operation;
            Order = order;
            Mode = mode;
        }
    }


    internal class EntityPlugin<TContext,T> : EntityPlugin
        where TContext : DynamicContext
        where T : DynamicEntity
    {



        // public static ConcurrentDictionary<Type, MethodInfo> Invokers = new ConcurrentDictionary<Type, MethodInfo>();

        public override async Task Execute(IServiceProvider services, ClaimsPrincipal principal, CollectionEntry collectionEntry)
        {
            foreach (var entity in collectionEntry.CurrentValue)
            {
                var plugincontext = new PluginContext<TContext, T>
                {
                    Input = entity as T,
                    DB =  collectionEntry.EntityEntry.Context as TContext,
                    User = principal,
                   
                      EntityResource = new EAVResource
                      {
                          EntityType = entity.GetType(),
                          EntityCollectionSchemaName = entity.GetType().GetCustomAttribute<EntityAttribute>().CollectionSchemaName
                      }
                };
                //var pluginContext = Activator.CreateInstance(typeof(PluginContext<,>).MakeGenericType(typeof(DBContext), entity.Entity.GetType()));

                var handler = services.GetService(Handler) as IPlugin<TContext, T>;
                await handler.Execute(plugincontext);
                //var invoker = Invokers.GetOrAdd(entityType, (t) => typeof(IPlugin<,>).MakeGenericType(t).GetMethod("Execute"));

                //var task = invoker.Invoke(handler, new object[] { pluginContext }) as Task;
                //await task;

               
            }
        }
        
        public override async Task<PluginContext> Execute(IServiceProvider services, ClaimsPrincipal principal, EntityEntry entity) 
        {
            
            var plugincontext = new PluginContext<TContext, T>
            {
                Input = entity.Entity as T,
                DB= entity.Context as TContext,
                User = principal,
                
                EntityResource = new EAVResource
                {
                    EntityType = entity.Entity.GetType(),
                    EntityCollectionSchemaName = entity.Entity.GetType().GetCustomAttribute<EntityAttribute>().CollectionSchemaName
                }
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
