using System;
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

        public IEnumerable<JToken> GetAttributeMetaData(string entity)
        {
            return _metaData.SelectTokens($".entities.{entity}.attributes").First();
        }
    }
    
    public interface IRetrieveMetaData
    {
        public IEnumerable<JToken> GetAttributeMetaData(string entity);
    }
}