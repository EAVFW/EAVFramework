using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Extensions;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Services;
using DotNetDevOps.Extensions.EAVFramework.Events;
using System.Collections.Generic;
using DotNetDevOps.Extensions.EAVFramework.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace DotNetDevOps.Extensions.EAVFramework.Hosting
{

    

    public interface IRecordValidator<T>
    {

    }


    
    public static class EndpointsMapping
    {
 
        public static void MapEAVFrameworkRoutes(this IEndpointRouteBuilder config)
        {
            var options = config.ServiceProvider.GetService<EAVFrameworkOptions>();
            var endpoints = config.ServiceProvider.GetService<IEnumerable<Endpoint>>();

            //var pipeline = config.CreateApplicationBuilder()
            //   .UseMiddleware<RoutingMiddleware>()
            //   .Build();

            foreach (var endpoint in endpoints)
            {
                config.MapMethods($"{options.RoutePrefix}{endpoint.Patten}".EnsureLeadingSlash(), endpoint.Methods,
                    context =>
                        context.RequestServices.GetService<IEndpointRouter>()
                        .ProcessAsync(context, context.RequestServices.GetService(endpoint.Handler) as IEndpointHandler))
                    .WithDisplayName(endpoint.Name)
                    .WithMetadata(endpoint);

                //config.MapMethods($"{options.RoutePrefix}{endpoint.Patten}".EnsureLeadingSlash(), endpoint.Methods,
                //  pipeline)
                //   .WithDisplayName(endpoint.Name)
                //   .WithMetadata(endpoint);
            }
            
        }
    }


   
}
