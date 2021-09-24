using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{

    public class RetrieveMetaData : IRetrieveMetaData
    {
        private readonly JToken _metaData;
       

        public RetrieveMetaData(IOptions<DynamicContextOptions> options)
        {
            _metaData = options.IsDefined() ? options.Value.Manifests.First() : throw new ArgumentNullException(nameof(options));
        }

        public IEnumerable<JProperty> GetAttributeMetaData(string entityLogicalName)
        {
            return _metaData.SelectToken("$.entities").OfType<JProperty>().FirstOrDefault(a => a.Value.SelectToken("$.logicalName")?.ToString() == entityLogicalName)?.Value.SelectToken("$.attributes").OfType<JProperty>();
        
        }
    }
    
    public interface IRetrieveMetaData
    {
        public IEnumerable<JProperty> GetAttributeMetaData(string entityLogicalName);
    }
}