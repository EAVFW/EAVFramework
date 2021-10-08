using System;
using System.Linq.Expressions;
using System.Security.Claims;
using DotNetDevOps.Extensions.EAVFramework.Validation;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public class PluginContext<TContext,T> : PluginContext
    {
      
        public T Input { get; set; }
        public TContext DB { get; set; }
        public ClaimsPrincipal ClaimsPrincipal { get; set; }

        public PluginContext<TContext, T> AddValidationError<TField>(Expression<Func<T, TField>> propExpression,
            string error, string attributeSchemaName)
        {
            Errors.Add(new ValidationError { Error = error, AttributeSchemaName = attributeSchemaName });

            return this;
        }

        public PluginContext<TContext, T> AddValidationError(Func<DynamicEntity, DynamicEntity> propExpression, ValidationError error)
        {
            Errors.Add(error);

            return this;
        }
    }
}
