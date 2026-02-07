using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EAVFramework.Validation
{

    public class RetrieveMetaData<TDynamicContext> : IRetrieveMetaData<TDynamicContext> where TDynamicContext : DynamicContext
    {
        private readonly IFormContextFeature<TDynamicContext> formContextFeature;

        public RetrieveMetaData(IFormContextFeature<TDynamicContext> formContextFeature)
        {
            this.formContextFeature = formContextFeature;
        }

        public async ValueTask<IEnumerable<JProperty>> GetAttributeMetaData(string entityLogicalName)
        {
            var _metaData = await formContextFeature.GetManifestAsync();
            return _metaData.SelectToken("$.entities").OfType<JProperty>().FirstOrDefault(a => a.Value.SelectToken("$.logicalName")?.ToString() == entityLogicalName)?.Value.SelectToken("$.attributes").OfType<JProperty>();
        }
    }
    
    public interface IRetrieveMetaData<TDynamicContext> where TDynamicContext : DynamicContext
    {
        public ValueTask<IEnumerable<JProperty>> GetAttributeMetaData(string entityLogicalName);
    }
}