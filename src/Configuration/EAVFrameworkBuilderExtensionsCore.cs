using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Configuration;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Extensions;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using DotNetDevOps.Extensions.EAVFramework.Services;
using DotNetDevOps.Extensions.EAVFramework.Services.Default;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder extension methods for registering core services
    /// </summary>
    public static class EAVFrameworkBuilderExtensionsCore
    {

        /// <summary>
        /// Creates a builder.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddEAVFrameworkBuilder(this IServiceCollection services)
        {
            return new EAVFrameworkBuilder(services);
        }


        /// <summary>
        /// Adds EAVFramework.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddEAVFramework<TContext>(this IServiceCollection services)
            where TContext : DynamicContext
        {
            var builder = services.AddEAVFrameworkBuilder();

            builder
                .AddCookieAuthentication()
                .AddRequiredPlatformServices()
                .AddDefaultEndpoints< TContext>()
                  .AddPluggableServices();
              

            

            return builder;
        }

        /// <summary>
        /// Adds EAVFramework.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="setupAction">The setup action.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddEAVFramework<TContext>(this IServiceCollection services, Action<EAVFrameworkOptions> setupAction)
             where TContext : DynamicContext
        {
            services.Configure(setupAction);
            return services.AddEAVFramework<TContext>();
        }

        /// <summary>
        /// Adds the EAVFramework.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddIdentityServer<TContext>(this IServiceCollection services, IConfiguration configuration)
             where TContext : DynamicContext
        {
            services.Configure<EAVFrameworkOptions>(configuration);
            return services.AddEAVFramework<TContext>();
        }


        /// <summary>
        /// Adds the default cookie handlers and corresponding configuration
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddCookieAuthentication(this IEAVFrameworkBuilder builder)
        {
            builder.Services.AddAuthentication(Constants.DefaultCookieAuthenticationScheme)
                .AddCookie(Constants.DefaultCookieAuthenticationScheme)
                .AddCookie(Constants.ExternalCookieAuthenticationScheme);

            builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureInternalCookieOptions>();
            builder.Services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureInternalCookieOptions>();
          //  builder.Services.AddTransientDecorator<IAuthenticationService, IdentityServerAuthenticationService>();
          //  builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, FederatedSignoutAuthenticationHandlerProvider>();

            return builder;
        }

        /// <summary>
        /// Adds the required platform services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddRequiredPlatformServices(this IEAVFrameworkBuilder builder)
        {
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddOptions();
            builder.Services.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<EAVFrameworkOptions>>().Value);
           
            builder.Services.AddHttpClient();

            return builder;
        }


        /// <summary>
        /// Adds the pluggable services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddPluggableServices(this IEAVFrameworkBuilder builder)
        {
            builder.Services.TryAddTransient<IEventService, DefaultEventService>();
            builder.Services.TryAddTransient<IEventSink, DefaultEventSink>();
            builder.Services.TryAddTransient<IPluginScheduler, DefaultPluginScheduler>();
            return builder;
        }

            /// <summary>
            /// Adds the default endpoints.
            /// </summary>
            /// <param name="builder">The builder.</param>
            /// <returns></returns>
            public static IEAVFrameworkBuilder AddDefaultEndpoints<TContext>(this IEAVFrameworkBuilder builder)
            where TContext : DynamicContext
        {

            builder.Services.AddTransient<IEndpointRouter, EndpointRouter>();

            builder.AddEndpoint<QueryRecordsEndpoint<TContext>>(EndpointNames.QueryRecords, RoutePatterns.QueryRecords.EnsureLeadingSlash(), HttpMethods.Get);
            builder.AddEndpoint<RetrieveRecordEndpoint<TContext>>(EndpointNames.RetrieveRecord, RoutePatterns.RecordPattern.EnsureLeadingSlash(), HttpMethods.Get);
            builder.AddEndpoint<CreateRecordsEndpoint<TContext>>(EndpointNames.CreateRecord, RoutePatterns.CreateRecord.EnsureLeadingSlash(), HttpMethods.Post);
            builder.AddEndpoint<PatchRecordsEndpoint<TContext>>(EndpointNames.PatchRecord, RoutePatterns.RecordPattern.EnsureLeadingSlash(), HttpMethods.Patch);
            builder.AddEndpoint<DeleteRecordEndpoint<TContext>>(EndpointNames.DeleteRecord, RoutePatterns.RecordPattern.EnsureLeadingSlash(), HttpMethods.Delete);

            return builder;
        }

        /// <summary>
        /// Adds the endpoint.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddEndpoint<T>(this IEAVFrameworkBuilder builder, string name, string pattern, params string[] methods)
            where T : class, IEndpointHandler
        {
            builder.Services.AddTransient<T>();
            builder.Services.AddSingleton(new DotNetDevOps.Extensions.EAVFramework.Hosting.Endpoint(name, pattern,methods, typeof(T)));

            return builder;
        }


        public static IEAVFrameworkBuilder AddPlugin<T,TContext,TEntity>(this IEAVFrameworkBuilder builder, EntityPluginExecution execution, EntityPluginOperation operation, int order=0)
            where T : class, IPlugin<TContext,TEntity>
            where TEntity : DynamicEntity
            where TContext : DynamicContext
        {
            builder.Services.AddTransient<T>();
            builder.Services.AddSingleton<EntityPlugin>(new EntityPlugin<TContext,TEntity> { Execution=execution, Operation = operation, Order=order, Type=typeof(TEntity), Handler=typeof(T) });

            return builder;
        }

        //public static IEAVFrameworkBuilder AddPlugin<T>(this IEAVFrameworkBuilder builder, EntityPluginExecution execution, int order = 0)
        //   where T : class, IPlugin
        //{
        //    builder.Services.AddTransient<T>();

             

        //    builder.Services.AddSingleton<EntityPlugin>(new EntityPlugin { Execution = execution, Order = order, Type = typeof(TEntity), Handler = typeof(T) });

        //    return builder;
        //}


    }

   

}
