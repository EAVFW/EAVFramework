using DotNetDevOps.Extensions.EAVFramework.Events;
using DotNetDevOps.Extensions.EAVFramework.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Hosting
{
    //public class RoutingMiddleware<TContext> where TContext : DynamicContext
    //{
    //    private readonly RequestDelegate _next;

    //    /// <summary>
    //    /// https://andrewlock.net/accessing-route-values-in-endpoint-middleware-in-aspnetcore-3/
    //    /// </summary>
    //    public RoutingMiddleware(RequestDelegate next)
    //    {
    //        _next = next;
    //    }
       
    //    public async Task InvokeAsync(HttpContext context, IEndpointRouter<TContext> endpointRouter)
    //    {
    //        var endpointFeature = context.Features[typeof(Microsoft.AspNetCore.Http.Features.IEndpointFeature)]
    //                                      as Microsoft.AspNetCore.Http.Features.IEndpointFeature;

    //        Microsoft.AspNetCore.Http.Endpoint endpoint = endpointFeature?.Endpoint;

    //        var eavEndpoint = endpoint.Metadata.GetMetadata<Endpoint<DynamicContext>>();

    //        var data = context.GetRouteData();
    //        await endpointRouter.ProcessAsync(context,context.RequestServices.GetService(eavEndpoint.Handler) as IEndpointHandler<TContext>);

    //        await _next(context);
    //    }
    //}
    public class EndpointRouter<TContext> : IEndpointRouter<TContext> where TContext : DynamicContext
    {
        private readonly IEventService _events;
        private readonly ILogger<EndpointRouter<TContext>> _logger;

        public EndpointRouter(IEventService events, ILogger<EndpointRouter<TContext>> logger)
        {
            _events = events;
            _logger = logger;
        }
        public async Task ProcessAsync(HttpContext httpContext, IEndpointHandler<TContext> endpoint)
        {
            try
            {


                _logger.LogInformation("Invoking EAVFramework endpoint: {endpointType} for {url}", endpoint.GetType().FullName, httpContext.Request.Path.ToString());

                var result = await endpoint.ProcessAsync(httpContext);

                if (result != null)
                {
                    _logger.LogTrace("Invoking result: {type}", result.GetType().FullName);
                    await result.ExecuteAsync(httpContext);
                }

                return;



            }
            catch (Exception ex)
            {
                await _events.RaiseAsync(new UnhandledExceptionEvent(ex));
                _logger.LogCritical(ex, "Unhandled exception: {exception}", ex.Message);
                throw;
            }
        }
    }
}
