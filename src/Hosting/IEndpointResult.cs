﻿using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramwork.Hosting
{
    /// <summary>
    /// Endpoint result
    /// </summary>
    public interface IEndpointResult
    {
        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        Task ExecuteAsync(HttpContext context);
    }
}
