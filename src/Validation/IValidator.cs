using EAVFramework.Plugins;
using Newtonsoft.Json.Linq;

namespace EAVFramework.Validation
{
    public interface IValidator<in T>
    {
        public bool ValidationPassed(T input, JToken manifest, out ValidationError error);
    }
}