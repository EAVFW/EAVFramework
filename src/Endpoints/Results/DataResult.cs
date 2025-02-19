using EAVFramework.Extensions;
using EAVFramework.Hosting;
using EAVFramework.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OData.Results;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class DataEndpointResult : IEndpointResult
    {
        private object data;

        public DataEndpointResult(object data)
        {
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            await context.Response.WriteJsonAsync(data,null, context.Request.Query.ContainsKey("pretty")? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }
    }
    public class ODataCollectionResult
    {

        [JsonProperty("@odata.context")]
        [JsonPropertyName("@odata.context")]
        public string Context { get; set; }
        [JsonProperty("@odata.count")]
        [JsonPropertyName("@odata.count")]
        public long? Count { get; set; }

        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public object[] Value { get; set; }
    }
    public class ODataEndpointResult : IEndpointResult
    {
        private readonly Type type;
        private PageResult data;

        public ODataEndpointResult(Type type, PageResult data)
        {
            this.type = type;
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
              
            await context.Response.WriteJsonAsync(new ODataCollectionResult { 
                Count= data.Count, 
                Context = $"{new Uri(context.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority)}/api/entities/$metadata#{type.GetCustomAttribute<EntityAttribute>().CollectionSchemaName}", //  "https://localhost:7093/api/entities/$metadata#AlgorithmOutputs(result,id)",
                Value = (data as IEnumerable).Cast<object>().ToArray()
            }, null,
                context.Request.Query.ContainsKey("pretty") ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }
    }
    
}
