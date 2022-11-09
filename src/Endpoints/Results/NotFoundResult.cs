using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class NotFoundResult : IEndpointResult
    {
       

        public NotFoundResult()
        {
          
        }

        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        }
    }
}
