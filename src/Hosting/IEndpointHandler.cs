using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Hosting
{
    /// <summary>
    /// Endpoint handler
    /// </summary>
    public interface IEndpointHandler<TContext> where TContext : DynamicContext
    {
        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        Task<IEndpointResult> ProcessAsync(HttpContext context);
    }
}
