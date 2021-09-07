using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{

    public class RetrieveMetaData : IRetrieveMetaData
    {
        private readonly JToken _metaData;

        public RetrieveMetaData(JToken metaData)
        {
            _metaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
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