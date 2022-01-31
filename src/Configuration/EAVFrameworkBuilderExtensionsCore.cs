using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Configuration;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Extensions;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using DotNetDevOps.Extensions.EAVFramework.Services;
using DotNetDevOps.Extensions.EAVFramework.Services.Default;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using DotNetDevOps.Extensions.EAVFramework.Authentication;
using DotNetDevOps.Extensions.EAVFramework.Validation;
using Microsoft.AspNetCore.Authentication;
using static DotNetDevOps.Extensions.EAVFramework.Constants;
using System.Threading.Tasks;
using System.Net;

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
            services.Configure<EAVFrameworkOptions>(o=> { });
            return new EAVFrameworkBuilder(services)
                  .AddRequiredPlatformServices();
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
                .AddDefaultEndpoints< TContext>()                
                  .AddPluggableServices();


          //  builder.Services.AddScoped<IPluginScheduler, DefaultPluginScheduler<TContext>>();



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
        /// Registers validation plugins, which uses validation rules from the Manifest to validate input and
        /// adds ValidationErrors to the context.
        /// Validation is run in PreValidation.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddValidation(this IEAVFrameworkBuilder builder)
        {
            builder.Services.TryAddScoped<IRetrieveMetaData, RetrieveMetaData>();
            
            builder.Services.RegisterValidator<StringValidator, string>();
            builder.Services.RegisterValidator<NumberValidator, decimal>();
            builder.Services.RegisterValidator<NumberValidator, int>();

            builder.AddPlugin<ValidationPlugin, DynamicContext, DynamicEntity>(
                EntityPluginExecution.PreValidate,
                EntityPluginOperation.Create);
            builder.AddPlugin<ValidationPlugin, DynamicContext, DynamicEntity>(
                EntityPluginExecution.PreValidate,
                EntityPluginOperation.Update);

            return builder;
        }

        /// <summary>
        /// Add generic check for required attributes, using manifest to determine if an attribute is required.
        /// This check is run as the last plugin in PreValidation. PreOperation is the stage where attributes are
        /// populated based on calculations or other fields. 
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="ignoredAttributes">List of attributes to be ignored when checking for required attributes</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddRequired(this IEAVFrameworkBuilder builder,
            List<string> ignoredAttributes = null)
        {
            builder.Services.TryAddScoped<IRetrieveMetaData, RetrieveMetaData>();

            if (ignoredAttributes != null)
            {
                builder.Services.Configure<RequiredSettings>(x => x.IgnoredFields = ignoredAttributes);
            }

            builder.AddPlugin<RequiredPlugin, DynamicContext, DynamicEntity>(
                EntityPluginExecution.PreValidate,
                EntityPluginOperation.Create,
                5);
            builder.AddPlugin<RequiredPlugin, DynamicContext, DynamicEntity>(
                EntityPluginExecution.PreValidate,
                EntityPluginOperation.Update,
                5);

            return builder;
        }

        /// <summary>
        /// Adds the default cookie handlers and corresponding configuration
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddCookieAuthentication(this IEAVFrameworkBuilder builder)
        {
            builder.Services.AddAuthentication(Constants.DefaultCookieAuthenticationScheme)
                .AddCookie(Constants.DefaultCookieAuthenticationScheme, options =>
                {
                    options.Events.OnRedirectToAccessDenied = UnAuthorizedResponse;
                })
                .AddCookie(Constants.ExternalCookieAuthenticationScheme)
                .AddCookie(Constants.DefaultLoginRedirectCookie);

            builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureInternalCookieOptions>();
            builder.Services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureInternalCookieOptions>();
          //  builder.Services.AddTransientDecorator<IAuthenticationService, IdentityServerAuthenticationService>();
          //  builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, FederatedSignoutAuthenticationHandlerProvider>();

            return builder;
        }
        
        internal static Task UnAuthorizedResponse(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return Task.CompletedTask;
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
            builder.Services.AddSingleton<IMigrationManager, MigrationManager>();
            builder.Services.AddHttpClient();
            builder.Services.AddEntityFrameworkSqlServer();
            builder.Services.AddScoped(typeof(EAVDBContext<>),typeof(EAVDBContext<>));
            builder.Services.AddSingleton<IODataConverterFactory, OdatatConverterFactory>();
        
            //builder.Services.AddSingleton<SavingIncepter>();
            return builder;
        }


        /// <summary>
        /// Adds the pluggable services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddPluggableServices(this IEAVFrameworkBuilder builder)
        {
            builder.Services.TryAddScoped(typeof(PluginsAccesser<>));
            builder.Services.TryAddTransient<IEventService, DefaultEventService>();
            builder.Services.TryAddTransient<IEventSink, DefaultEventSink>();
            builder.Services.TryAddTransient(typeof(IPluginScheduler<>), typeof(DefaultPluginScheduler<>));
            builder.Services.TryAddTransient<IPermissionStore, DefaultPermissionStore>();
            builder.Services.TryAddTransient<IFormContextFeature, DefaultFormContextFeature>();
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
            builder.AddEndpoint<QueryEntityPermissionsEndpoint<TContext>>(EndpointNames.QueryEntityPermissions, RoutePatterns.QueryEntityPermissions.EnsureLeadingSlash(), HttpMethods.Get);
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

        public static AuthenticatedEAVFrameworkBuilder AddAuthentication(
            this IEAVFrameworkBuilder builder,
            AuthenticationProperties properties = null)
        {
            var props = properties ?? new AuthenticationProperties();
            builder.Services.AddTransient<AuthenticationProperties>(_ => props);
            builder.AddCookieAuthentication();
            return new AuthenticatedEAVFrameworkBuilder(builder);
        }

        public static AuthenticatedEAVFrameworkBuilder AddAuthenticationProvider<T,TOptions>(
            this AuthenticatedEAVFrameworkBuilder builder,
            Action<TOptions> configureOptions) where T: class, IEasyAuthProvider
            where TOptions:class
        {
            var at = Activator.CreateInstance<T>();
            builder.Services.Configure(configureOptions);
            builder.Services.TryAddScoped<T>();
            builder.Services.AddScoped<IEasyAuthProvider>(sp=>sp.GetRequiredService<T>());
            var name = at.AuthenticationName;
            builder.Services.AddAuthentication()
                .AddCookie(name, o=>
                {
                    
                    o.LoginPath = "/account/login";
                });
            return builder;
        }

        public static IEAVFrameworkBuilder AddPlugin<T,TContext,TEntity>(this IEAVFrameworkBuilder builder, EntityPluginExecution execution, EntityPluginOperation operation, int order=0, EntityPluginMode mode = EntityPluginMode.Sync)
            where T : class, IPlugin<TContext,TEntity>
            where TEntity : DynamicEntity
            where TContext : DynamicContext
        {
            builder.Services.AddTransient<T>();
            builder.Services.AddSingleton<EntityPlugin>(new EntityPlugin<TContext,TEntity> { Execution=execution, Operation = operation, Order=order, Type=typeof(TEntity), Handler=typeof(T), Mode =mode  });

            return builder;
        }

        /// <summary>
        /// Add a validator to validate form payload per type when .AddValidation() is called.
        /// </summary>
        /// <typeparam name="TValidation">IValidator<> handler</typeparam>
        /// <typeparam name="TType">Type being validated</typeparam>
        public static IEAVFrameworkBuilder RegisterValidator<TValidation, TType>(this IEAVFrameworkBuilder builder) where TValidation : class
        {
            builder.Services.AddSingleton<TValidation>();
            builder.Services.AddSingleton<ValidatorMetaData>(new ValidatorMetaData<TType>
            {
                Handler = typeof(TValidation)
            });
            return builder;
        }
    }
}
