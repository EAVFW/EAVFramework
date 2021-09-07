using System;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    /// <summary>
    /// We have this general class to be able to resolve all validators for all types at once.
    /// </summary>
    public abstract class ValidatorMetaData
    {
        public Type Type { get; protected set; }

        public abstract bool ValidationPassed(IServiceProvider serviceProvider, object input, JToken manifest, out string error);
    }

    /// <summary>
    /// Why is this a generic class?
    /// We cannot combine the above class and this class, due to issues with type of input. When calling ValidationPassed
    /// the type of input is object due to C# abstraction in ValidationPlugin.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValidatorMetaData<T> : ValidatorMetaData
    {
        public Type Handler { get; set; }

        public ValidatorMetaData()
        {
            Type = typeof(T);   
        }

        public override bool ValidationPassed(IServiceProvider serviceProvider, object input, JToken manifest, out string error)
        {
            if (serviceProvider.GetService(Handler) is IValidator<T> handler)
            {
                return handler.ValidationPassed((T) input, manifest, out error);
            }
            
            throw new Exception($"Validation handler {Handler} could not be found");
        }
    }
}