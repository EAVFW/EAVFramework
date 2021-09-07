using System;
using System.Linq.Expressions;
using System.Security.Claims;

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
    }
}
