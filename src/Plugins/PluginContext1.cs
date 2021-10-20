using System;
using System.Linq.Expressions;
using System.Security.Claims;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Validation;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
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
