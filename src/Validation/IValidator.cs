using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public interface IValidator<in T>
    {
        public bool ValidationPassed(T input, JToken manifest, out string error);
    }
}