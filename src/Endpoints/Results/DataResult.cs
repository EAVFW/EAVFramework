using DotNetDevOps.Extensions.EAVFramework.Extensions;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints.Results
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
    public class AuthorizationEndpointResult : IEndpointResult
    {
        private object data;

        public AuthorizationEndpointResult(object data)
        {
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
           // context.Features.Get<IHttpResponseFeature>().ReasonPhrase = "Fai;
            context.Response.StatusCode = 401;
            
            await context.Response.WriteJsonAsync(data, null, context.Request.Query.ContainsKey("pretty") ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }
    }
}
