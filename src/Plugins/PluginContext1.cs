using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using DotNetDevOps.Extensions.EAVFramework.Validation;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public static class PluginContextFactory
    {
        public static PluginContext<TContext,T> CreateContext<TContext,T>(TContext context, EntityEntry entry, ClaimsPrincipal user )
        {
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
            return plugincontext;
        }
        public static PluginContext CreateContext<TContext>(TContext context, EntityEntry entry, ClaimsPrincipal user, Type recordtype)
        {
           var method= typeof(PluginContextFactory).GetMethod(nameof(CreateContext), 2, new Type[] { typeof(TContext), typeof(EntityEntry), typeof(ClaimsPrincipal) });
            return (PluginContext)method.MakeGenericMethod(typeof(TContext), recordtype).Invoke(null,new object[] { context,entry,user });
        }
    }
    public class PluginContext<TContext,T> : PluginContext
    {
      
        public T Input { get; set; }
        public TContext DB { get; set; }
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
