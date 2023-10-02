using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EAVFramework
{
    public class DefaultFormContextFeature<TDynamicContext> :IFormContextFeature<DynamicContext> where TDynamicContext : DynamicContext
    {
        private readonly IOptions<DynamicContextOptions> options;

        public DefaultFormContextFeature(IOptions<DynamicContextOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ValueTask<JToken> GetManifestAsync()
        {
            return new ValueTask<JToken>(options.Value.Manifests.First());
        }
    }
}
