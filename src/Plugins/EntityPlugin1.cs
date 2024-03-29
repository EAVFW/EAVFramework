﻿using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class PluginExtensions
    {
        public static IServiceCollection AddPlugin<T>(this IServiceCollection services)
        where T : class, IPluginRegistration
        {

            return services.AddPlugin(typeof(T));
        }

        public static IServiceCollection AddPlugin(this IServiceCollection services, Type pluginType)
        {

            services.AddTransient(pluginType);


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


                services.AddSingleton(entry);
            }

            return services;
        }
    }
}
namespace EAVFramework.Plugins
{
   
    public interface IPluginRegistration
    {

    }

    public static class PluginAutoReg
    {
        public static IEAVFrameworkBuilder AddPlugin<T>(this IEAVFrameworkBuilder builder)
            where T : class, IPluginRegistration
        {

            return builder.AddPlugin(typeof(T));
        }

     
        public static IEAVFrameworkBuilder AddPlugin(this IEAVFrameworkBuilder builder, Type pluginType)
        {
             builder.Services.AddPlugin(pluginType);
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

    public class PluginContextAccessor
    {
        public PluginContext Context { get; internal set; }
    }

    public class EntityPlugin<TContext,T> : EntityPlugin<TContext>
        where TContext : DynamicContext
        where T : DynamicEntity
    {



        // public static ConcurrentDictionary<Type, MethodInfo> Invokers = new ConcurrentDictionary<Type, MethodInfo>();

        public override async Task Execute(IServiceProvider services, ClaimsPrincipal principal, CollectionEntry collectionEntry)
        {
            var db = services.GetRequiredService<EAVDBContext<TContext>>();
            var contextWrapper = services.GetRequiredService<PluginContextAccessor>();
            foreach (var entity in collectionEntry.CurrentValue)
            {

                var plugincontext = new PluginContext<TContext, T>
                {
                    Input = entity as T,
                    DB = db,
                    User = principal,
                   
                      EntityResource = new EAVResource
                      {
                          EntityType = entity.GetType(),
                          EntityCollectionSchemaName = entity.GetType().GetCustomAttribute<EntityAttribute>().CollectionSchemaName
                      }
                };

                contextWrapper.Context = plugincontext;
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
            var db = services.GetRequiredService<EAVDBContext<TContext>>();
           
            var plugincontext = PluginContextFactory.CreateContext<TContext, T>(services,db, entity, principal);
           
            var handler = services.GetService(Handler) as IPlugin<TContext, T>;
            await handler.Execute(plugincontext);
            
            return plugincontext;
        }
    }
}
