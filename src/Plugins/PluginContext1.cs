using System;
using System.Linq.Expressions;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public class PluginContext<TContext,T> : PluginContext
    {
      
        public T Input { get; set; }
        public TContext DB { get; set; }


        public PluginContext<TContext, T> AddValidationError<TField>(Expression<Func<T,TField>> propExpression,string error)
        {
            Errors.Add(new ValidationError { Error = error });

            return this;
        }
    }
}
