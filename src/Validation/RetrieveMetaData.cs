using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{

    public class RetrieveMetaData : IRetrieveMetaData
    {
        private readonly IFormContextFeature formContextFeature;

        public RetrieveMetaData(IFormContextFeature formContextFeature)
        {
            this.formContextFeature = formContextFeature;
        }

        public async ValueTask<IEnumerable<JProperty>> GetAttributeMetaData(string entityLogicalName)
        {
            var _metaData = await formContextFeature.GetManifestAsync();
            return _metaData.SelectToken("$.entities").OfType<JProperty>().FirstOrDefault(a => a.Value.SelectToken("$.logicalName")?.ToString() == entityLogicalName)?.Value.SelectToken("$.attributes").OfType<JProperty>();
        }
    }
    
    public interface IRetrieveMetaData
    {
        public ValueTask<IEnumerable<JProperty>> GetAttributeMetaData(string entityLogicalName);
    }
}