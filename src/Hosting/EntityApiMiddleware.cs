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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace DotNetDevOps.Extensions.EAVFramework.Hosting
{

    

    public interface IRecordValidator<T>
    {

    }


 
    public static class EndpointsMapping
    {
 
        public static void MapEAVFrameworkRoutes<TContext>(this IEndpointRouteBuilder config, bool addAuth=true) where TContext : DynamicContext
        {
            var options = config.ServiceProvider.GetService<EAVFrameworkOptions>();
            var endpoints = config.ServiceProvider.GetService<IEnumerable<Endpoint<TContext>>>();

            //var pipeline = config.CreateApplicationBuilder()
            //   .UseMiddleware<RoutingMiddleware>()
            //   .Build();

            foreach (var endpoint in endpoints)
            {
                var endpointConfig = config.MapMethods($"{options.RoutePrefix}{endpoint.Patten}".EnsureLeadingSlash(), endpoint.Methods,
                    context =>
                        context.RequestServices.GetService<IEndpointRouter<TContext>>()
                        .ProcessAsync(context, context.RequestServices.GetService(endpoint.Handler) as IEndpointHandler<TContext>))
                    .WithDisplayName(endpoint.Name)
                    .WithMetadata(endpoint);
                
                options.Endpoints.EndpointAuthorizationConfiguration?.Invoke(endpointConfig);

                options.Endpoints.EndpointConfiguration?.Invoke(endpointConfig);
                //config.MapMethods($"{options.RoutePrefix}{endpoint.Patten}".EnsureLeadingSlash(), endpoint.Methods,
                //  pipeline)
                //   .WithDisplayName(endpoint.Name)
                //   .WithMetadata(endpoint);
            }

            if (addAuth)
            {
                var authProps = config.ServiceProvider.GetService<AuthenticationProperties>();

                config.AddEasyAuth(authProps ?? new AuthenticationProperties());
            }
        }
    }


   
}
