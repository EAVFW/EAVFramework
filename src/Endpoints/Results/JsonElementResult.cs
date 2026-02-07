using EAVFramework.Extensions;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class JsonElementResult : IEndpointResult
    {
        private JsonElement data;

        public JsonElementResult(JsonElement data)
        {
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            await context.Response.WriteJsonAsync(data.GetRawText());
        }
    }
}
