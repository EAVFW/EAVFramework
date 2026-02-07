using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class RedirectResult : IEndpointResult
    {
        private readonly string url;

        public RedirectResult(string url)
        {
            this.url = url;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.Redirect(url);
            return Task.CompletedTask;
        }
    }
}
