using EAVFramework;
using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
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
        public static IServiceCollection AddDynamicContextPlugin<TContext>(this IServiceCollection services, Type pluginType)
            where TContext : DynamicContext
        {
            services.AddTransient(pluginType);

            foreach (var attr in pluginType.GetCustomAttributes<PluginRegistrationAttribute>())
            {

                var plugin = pluginType.GetInterface("IPlugin`2").GenericTypeArguments;

                var contexttype = plugin[0];
                var entitytype = plugin[1];



                var entry = new DynamicEntityPlugin<TContext>(); // (EntityPlugin) Activator.CreateInstance(typeof(DynamicEntityPlugin<>).MakeGenericType(contexttype));

                entry.Execution = attr.Execution;
                entry.Mode = attr.Mode;
                entry.Order = attr.Order;
                entry.Operation = attr.Operation;
                entry.Type = entitytype;
                entry.Handler = pluginType;


                services.AddSingleton<EntityPlugin>(entry);
            }

            return services;
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

    public class DynamicEntityPlugin<TContext> : EntityPlugin<TContext>
        where TContext : DynamicContext
       // where T : DynamicEntity
    {

       

        public override ValueTask<bool> ShouldPluginBeExecued<T>(T context, TrackedPipelineItem entity)
         
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (entity.Entity is null)
            {
                throw new ArgumentNullException("Entry");
            }
            if (entity.Entity.Entity is null)
            {
                throw new ArgumentNullException("Entity");
            }
            /**
             * A plugin that is registered with a interface against all entities with * matching can be catched here first.
             */
            if (Type.IsGenericTypeParameter)
            { 
                var interfaceType = Type.GetGenericParameterConstraints().FirstOrDefault(c => c.IsInterface);               

                if (interfaceType.IsAssignableFrom(entity.Entity.Entity.GetType()) || entity.Entity.Entity.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)){
                    return ValueTask.FromResult(true);
                }
            }

                
            var type = GetPluginType(context as TContext);
        

            return ValueTask.FromResult(type?.IsAssignableFrom(entity.Entity.Entity.GetType())??false);
          

          
        }
        public override async Task Execute(IServiceProvider services, ClaimsPrincipal principal, CollectionEntry collectionEntry)

        {
            var type = GetPluginType(services.GetRequiredService<TContext>());

            var task = this.GetType().GetMethod(nameof(Execute), 1,
                new Type[] { typeof(IServiceProvider), typeof(ClaimsPrincipal), typeof(CollectionEntry) }).MakeGenericMethod(type)
                .Invoke(this, new object[] { services, principal, collectionEntry }) as Task;
            await task;

        }

        private Type GetPluginType(TContext db)
        {
             
            if (Type.IsGenericTypeParameter)
            {
              
                var interfaceType = Type.GetGenericParameterConstraints().FirstOrDefault(c => c.IsInterface);

                return db.Manager.ModelDefinition.EntityDTOs.Values.FirstOrDefault(t => t.GetInterfaces().Any(i => i==interfaceType || (i.IsGenericType && interfaceType.IsGenericType && i.GetGenericTypeDefinition() == interfaceType.GetGenericTypeDefinition())));


            }
            return Type;
        }

        public async  Task Execute<T>(IServiceProvider services, ClaimsPrincipal principal, CollectionEntry collectionEntry) where T:DynamicEntity
           
        {
            
            var db = services.GetRequiredService<EAVDBContext<TContext>>();
            var contextWrapper = services.GetRequiredService<PluginContextAccessor>();
            foreach (var entity in collectionEntry.CurrentValue)
            {
             //   var plugincontext = PluginContextFactory.CreateContext<TContext, T>(services, db, entity, principal);
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

                var handler = services.GetDynamicService<TContext>(Handler) as IPlugin<TContext, T>;
                await handler.Execute(plugincontext);
                //var invoker = Invokers.GetOrAdd(entityType, (t) => typeof(IPlugin<,>).MakeGenericType(t).GetMethod("Execute"));

                //var task = invoker.Invoke(handler, new object[] { pluginContext }) as Task;
                //await task;


            }
        }
        public override async Task<PluginContext> Execute(IServiceProvider services, ClaimsPrincipal principal, EntityEntry entity)

        {
            //var type =  GetPluginType(services.GetRequiredService<TContext>());

            var task = this.GetType().GetMethod(nameof(Execute), 1,
              new Type[] { typeof(IServiceProvider), typeof(ClaimsPrincipal), typeof(EntityEntry) }).MakeGenericMethod(entity.Entity.GetType())
              .Invoke(this, new object[] { services, principal, entity }) as Task<PluginContext>;
            return await task;
        }
        public async Task<PluginContext> Execute<T>(IServiceProvider services, ClaimsPrincipal principal, EntityEntry entity) where T:DynamicEntity
            
        {
            var db = services.GetRequiredService<EAVDBContext<TContext>>();

            var plugincontext = PluginContextFactory.CreateContext<TContext, T>(services, db, entity, principal, this.Operation);

            var handler = services.GetDynamicService<TContext>(Handler, Type.IsGenericTypeParameter ? new[] { (Type,typeof(T))} : new (Type,Type)[0] ) as IPlugin<TContext, T>;
            await handler.Execute(plugincontext);

            return plugincontext;
        }
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
           
            var plugincontext = PluginContextFactory.CreateContext<TContext, T>(services,db, entity, principal,this.Operation);
           
            var handler = services.GetService(Handler) as IPlugin<TContext, T>;
            await handler.Execute(plugincontext);
            
            return plugincontext;
        }
    }
}
