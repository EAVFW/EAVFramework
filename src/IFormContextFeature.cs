using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace EAVFramework
{
    public interface IFormContextFeature<TDynamicContext> where TDynamicContext : DynamicContext
    {
        public ValueTask<JToken> GetManifestAsync();
    }
}
