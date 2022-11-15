using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using EAVFramework.Endpoints;
using EAVFramework.Shared;
using EAVFramework.Validation;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace EAVFramework.Plugins
{
    public static class PluginContextFactory
    {
        public static PluginContext<TContext,T> CreateContext<TContext,T>(IServiceProvider services,EAVDBContext<TContext> context, EntityEntry entry, ClaimsPrincipal user )
            where TContext : DynamicContext
        {
            var contextWrapper = services.GetRequiredService<PluginContextAccessor>();
            var plugincontext = new PluginContext<TContext, T>
            {
                Input = (T)entry.Entity,
                DB = context,
                User = user,
                EntityResource = new EAVResource
                {
                    EntityType = entry.Entity.GetType(),
                    EntityCollectionSchemaName = entry.Entity.GetType().GetCustomAttribute<EntityAttribute>().CollectionSchemaName
                }
            };
            contextWrapper.Context = plugincontext;
            return plugincontext;
        }
        public static PluginContext CreateContext<TContext>(TContext context, EntityEntry entry, ClaimsPrincipal user, Type recordtype)
        {
           var method= typeof(PluginContextFactory).GetMethod(nameof(CreateContext), 2, new Type[] { typeof(TContext), typeof(EntityEntry), typeof(ClaimsPrincipal) });
            return (PluginContext)method.MakeGenericMethod(typeof(TContext), recordtype).Invoke(null,new object[] { context,entry,user });
        }
    }
    public class PluginContext<TContext,T> : PluginContext
        where TContext : DynamicContext
    {
      
        public T Input { get; set; }
        public EAVDBContext<TContext> DB { get; set; }
        public ClaimsPrincipal User { get; set; }
      
        public EAVResource EntityResource { get; internal set; }

        public PluginContext<TContext, T> AddValidationError<TField>(Expression<Func<T, TField>> propExpression,
            string error, string attributeSchemaName)
        {
            Errors.Add(new ValidationError { Error = error, AttributeSchemaName = attributeSchemaName, EntityCollectionSchemaName = EntityResource.EntityCollectionSchemaName });

            return this;
        }

        public PluginContext<TContext, T> AddValidationError(ValidationError error)
        {
            Errors.Add(error);

            return this;
        }
    }
}
