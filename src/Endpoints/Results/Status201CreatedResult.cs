using EAVFramework.Extensions;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class Status201CreatedResult : IEndpointResult
    {
        private object data;

        public Status201CreatedResult(object data)
        {
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status201Created;
            await context.Response.WriteJsonAsync(data, null, context.Request.Query.ContainsKey("pretty") ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }
    }
}
