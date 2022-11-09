using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class Status202AcceptedResult : IEndpointResult
    {
        

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
          
        }
    }
}
