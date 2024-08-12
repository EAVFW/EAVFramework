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
using EAVFramework.Extensions;
using EAVFramework.Hosting;
using EAVFramework.Services;
using EAVFramework.Events;
using System.Collections.Generic;
using EAVFramework.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace EAVFramework.Hosting
{



    public interface IRecordValidator<T>
    {

    }



    public static class EndpointsMapping
    {

        public static void MapEAVFrameworkRoutes<TContext>(this IEndpointRouteBuilder config, bool withAuth=true)
           where TContext : DynamicContext
        {
            var options = config.ServiceProvider.GetService<EAVFrameworkOptions>();
            var endpoints = config.ServiceProvider.GetService<IEnumerable<Endpoint<TContext>>>();

            //var pipeline = config.CreateApplicationBuilder()
            //   .UseMiddleware<RoutingMiddleware>()
            //   .Build();

            foreach (var endpoint in endpoints)
            {
                var endpointConfig = config.MapMethods($"{(endpoint.RoutePrefixIgnored ? String.Empty : options.RoutePrefix)}{endpoint.Patten}".EnsureLeadingSlash(), endpoint.Methods,
                    context =>
                        context.RequestServices.GetService<IEndpointRouter<TContext>>()
                        .ProcessAsync(context, context.RequestServices.GetService(endpoint.Handler) as IEndpointHandler<TContext>))
                    .WithDisplayName(endpoint.Name)
                    .WithMetadata(endpoint);

                if (endpoint.Metadata.Any())
                {
                    endpointConfig.WithMetadata(items: endpoint.Metadata.ToArray());
                }


                options.Endpoints.EndpointAuthorizationConfiguration?.Invoke(endpointConfig);

                options.Endpoints.EndpointConfiguration?.Invoke(endpointConfig);
                //config.MapMethods($"{options.RoutePrefix}{endpoint.Patten}".EnsureLeadingSlash(), endpoint.Methods,
                //  pipeline)
                //   .WithDisplayName(endpoint.Name)
                //   .WithMetadata(endpoint);
            }

            if (options.Authentication.EnableEasyAuth && withAuth)
            {
                var authProps = config.ServiceProvider.GetService<AuthenticationProperties>();
                config.AddEasyAuth();
            }


        }

       
    }



}
