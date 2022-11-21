using EAVFramework.Extensions;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
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
