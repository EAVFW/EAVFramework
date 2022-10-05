using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Hosting
{

    /// <summary>
    /// The endpoint router
    /// </summary>
    public interface IEndpointRouter<TContext> where TContext : DynamicContext
    {
        /// <summary>
        /// Proccesses a matching endpoint.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="endpoint">The endpoint to execute</param>
        /// <returns></returns>
        Task ProcessAsync(HttpContext httpContext, IEndpointHandler<TContext> endpoint);
    }
}
