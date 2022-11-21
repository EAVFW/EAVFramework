using EAVFramework.Extensions;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
}
