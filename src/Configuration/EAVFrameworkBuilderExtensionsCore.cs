using EAVFramework;
using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFramework.Extensions;
using EAVFramework.Hosting;
using EAVFramework.Plugins;
using EAVFramework.Services;
using EAVFramework.Services.Default;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using EAVFramework.Authentication;
using EAVFramework.Validation;
using Microsoft.AspNetCore.Authentication;
using static EAVFramework.Constants;
using System.Threading.Tasks;
using System.Net;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using EAVFramework.Shared.V2;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static EAVFramework.Shared.TypeHelper;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Linq.Expressions;
using System.Linq;
using EAVFramework.Shared;
using EAVFramework.Endpoints.Query.OData;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class DynamicCodeServiceExtensions
    {
        public static IServiceCollection AddCodeServices(this IServiceCollection services)
        {
            services.AddSingleton<IMigrationManager, MigrationManager>();
            services.AddOptions<MigrationManagerOptions>().Configure<IOptions<DynamicContextOptions>>((o, oo) =>
            {
                o.CopyFrom(oo.Value);
            });
            services.AddSingleton<IDynamicCodeServiceFactory, DynamicCodeServiceFactory>();
            return services;
        }
    }

  


    public static class GenericTypeExtensions
    {
        public static Type ResolveGenericArguments<TContext,TModel>(this Type t) where TContext : DynamicContext
        {
            var ttt = t.GetGenericArguments().Select(ta =>
            {
                var constraints = ta.GetGenericParameterConstraints();
                if (constraints.Any(tta => tta == typeof(DynamicContext)))
                {
                    return typeof(TContext);
                }

                if(constraints.Any(tta => tta == typeof(IConvertible)))
                {
                    //enums
                    var constraintMapping = t.GetCustomAttributes<ConstraintMappingAttribute>().SingleOrDefault(c => c.ConstraintName == ta.Name)
                    ?? throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {ta.Name} on {t.Name}");



                    var entirtyType = typeof(TModel).Assembly.GetTypes().SingleOrDefault(t => 
                    t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute &&
                    t.GetCustomAttribute<EntityAttribute>() is EntityAttribute attr && attr.EntityKey == constraintMapping.EntityKey);

                    var propertyType = entirtyType.GetProperties().Single(c =>
                        c.GetCustomAttribute<EntityFieldAttribute>() is EntityFieldAttribute field && field.AttributeKey == constraintMapping.AttributeKey);

                    return Nullable.GetUnderlyingType(propertyType.PropertyType) ?? propertyType.PropertyType;


                }

                var @interface = constraints.FirstOrDefault(c => c.IsInterface && !string.IsNullOrEmpty(c.GetCustomAttribute<EntityInterfaceAttribute>()?.EntityKey));
                if (@interface != null)
                {

                    if (@interface.IsGenericType)
                    {
                        return typeof(TModel).Assembly.GetTypes().Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                       t.GetInterfaces().Any(c => c.IsGenericType && c.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition())).Single();
                    }


                    return typeof(TModel).Assembly.GetTypes().Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                            t.GetInterfaces().Any(c => c == @interface)).Single();
                }

                throw new InvalidOperationException($"Cant find constraint for {ta.Name} on {t.Name}" );

            }).ToArray();
            return t.MakeGenericType(ttt);
        }
    }

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
        public static IEAVFrameworkBuilder AddEAVFrameworkBuilder<TContext>(this IServiceCollection services, string schema, string connectionString)
            where TContext : DynamicContext
        {
         
            services.Configure<EAVFrameworkOptions>(o=> {
                o.Schema = schema;
                o.ConnectionString = connectionString;
            });
            return new EAVFrameworkBuilder<TContext>(services, schema,connectionString)
                  .AddRequiredPlatformServices();
        }


        /// <summary>
        /// Adds EAVFramework.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddEAVFramework<TContext>(this IServiceCollection services, 
            string schema="dbo", string connectionString= "Name=ApplicationDB"
            )
            where TContext : DynamicContext
        {
            var builder = services.AddEAVFrameworkBuilder<TContext>(schema,connectionString);

            builder              
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
        /// Registers validation plugins, which uses validation rules from the Manifest to validate input and
        /// adds ValidationErrors to the context.
        /// Validation is run in PreValidation.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IEAVFrameworkBuilder AddValidation<TDynamicContext>(this IEAVFrameworkBuilder builder) where TDynamicContext:DynamicContext
        {
            builder.Services.TryAddScoped(typeof(IRetrieveMetaData<>),typeof(RetrieveMetaData<>));
            
            builder.Services.RegisterValidator<StringValidator, string>();
            builder.Services.RegisterValidator<NumberValidator, decimal>();
            builder.Services.RegisterValidator<NumberValidator, int>();

            builder.AddPlugin<ValidationPlugin<TDynamicContext>, TDynamicContext, DynamicEntity>(
                EntityPluginExecution.PreValidate,
                EntityPluginOperation.Create);
            builder.AddPlugin<ValidationPlugin<TDynamicContext>, TDynamicContext, DynamicEntity>(
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
            builder.Services.TryAddScoped(typeof(IRetrieveMetaData<>), typeof(RetrieveMetaData<>));

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

                  
                    options.Events.OnRedirectToAccessDenied = ReplaceRedirector(HttpStatusCode.Forbidden, options.Events.OnRedirectToAccessDenied);
                    options.Events.OnRedirectToLogin = ReplaceRedirector(HttpStatusCode.Unauthorized, options.Events.OnRedirectToLogin);


                })
                .AddCookie(Constants.ExternalCookieAuthenticationScheme)
                .AddCookie(Constants.DefaultLoginRedirectCookie);
          
            builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureInternalCookieOptions>();
            builder.Services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureInternalCookieOptions>();
          //  builder.Services.AddTransientDecorator<IAuthenticationService, IdentityServerAuthenticationService>();
          //  builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, FederatedSignoutAuthenticationHandlerProvider>();

            return builder;
        }

        static Func<RedirectContext<CookieAuthenticationOptions>, Task> ReplaceRedirector(HttpStatusCode statusCode, Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector) =>
    context => {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = (int)statusCode;
            return Task.CompletedTask;
        }
        return existingRedirector(context);
    };

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
            builder.Services.AddEntityFrameworkSqlServer();
            builder.Services.AddScoped(typeof(EAVDBContext<>),typeof(EAVDBContext<>));
            builder.Services.AddSingleton<IODataConverterFactory, OdatatConverterFactory>();
            builder.Services.AddCodeServices();
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
            builder.Services.TryAddScoped<PluginContextAccessor>();
            builder.Services.TryAddScoped(typeof(PluginsAccesser<>));
            builder.Services.TryAddTransient<IEventService, DefaultEventService>();
            builder.Services.TryAddTransient<IEventSink, DefaultEventSink>();
            builder.Services.TryAddTransient(typeof(IPluginScheduler<>), typeof(DefaultPluginScheduler<>));
            builder.Services.TryAddTransient(typeof(IPermissionStore<>), typeof(DefaultPermissionStore<>));
            builder.Services.TryAddTransient<IFormContextFeature<DynamicContext>, DefaultFormContextFeature<DynamicContext>>();
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

            builder.Services.AddTransient<IEndpointRouter<TContext>, EndpointRouter<TContext>>();
         
           
            builder.AddEndpoint<QueryRecordsEndpoint<TContext>, TContext>(EndpointNames.QueryRecords, RoutePatterns.QueryRecords.EnsureLeadingSlash(), HttpMethods.Get);
            builder.AddEndpoint<RetrieveRecordEndpoint<TContext>, TContext>(EndpointNames.RetrieveRecord, RoutePatterns.RecordPattern.EnsureLeadingSlash(), HttpMethods.Get);
            builder.AddEndpoint<CreateRecordsEndpoint<TContext>, TContext>(EndpointNames.CreateRecord, RoutePatterns.CreateRecord.EnsureLeadingSlash(), HttpMethods.Post);
            builder.AddEndpoint<QueryEntityPermissionsEndpoint<TContext>, TContext>(EndpointNames.QueryEntityPermissions, RoutePatterns.QueryEntityPermissions.EnsureLeadingSlash(), HttpMethods.Get);
            builder.AddEndpoint<PatchRecordsEndpoint<TContext>, TContext>(EndpointNames.PatchRecord, RoutePatterns.RecordPattern.EnsureLeadingSlash(), HttpMethods.Patch);
            builder.AddEndpoint<DeleteRecordEndpoint<TContext>, TContext>(EndpointNames.DeleteRecord, RoutePatterns.RecordPattern.EnsureLeadingSlash(), HttpMethods.Delete);

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
        public static IEndpointBuilder AddEndpoint<T,TContext>(this IEAVFrameworkBuilder builder, string name, string pattern, params string[] methods)
            where T : class, IEndpointHandler<TContext>
            where TContext : DynamicContext
        {
            return builder.Services.AddEndpoint<T, TContext>(name,pattern,methods);
             
         
        }
      

        public static IEndpointBuilder AddEndpoint<T, TContext>(this IServiceCollection services, string name, string pattern, params string[] methods)
           where T : class, IEndpointHandler<TContext>
           where TContext : DynamicContext
        {
            return services.AddEndpoint<TContext>(typeof(T),name,pattern,methods);
            

          
        }


        public static IEndpointBuilder AddEndpoint<TContext>(this IServiceCollection services, Type endpoint)
               where TContext : DynamicContext
        {
            var attr = endpoint.GetCustomAttribute<EndpointRouteAttribute>();

            return services.AddEndpoint<TContext>(endpoint,attr.Name, attr.Route,
                endpoint.GetCustomAttributes<EndpointRouteMethodAttribute>(true).Select(c=>c.Method).ToArray());


        }
        public static IEndpointBuilder AddEndpoint<TContext>(this IServiceCollection services,Type endpoint,  string name, string pattern, params string[] methods)
          
          where TContext : DynamicContext
        {
            services.AddTransient(endpoint);

            var builder = new EAVFramework.Hosting.Endpoint<TContext>(name, pattern, methods, endpoint);
            services.AddSingleton(builder);

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
             
            builder.Services.AddOptions<TOptions>().Configure(configureOptions);

            return builder.AddAuthenticationProvider<T>();
        }

        private static AuthenticatedEAVFrameworkBuilder AddAuthenticationProvider<T>(this AuthenticatedEAVFrameworkBuilder builder) where T : class, IEasyAuthProvider
        {
            var at = Activator.CreateInstance<T>();
            builder.Services.TryAddScoped<T>();
            builder.Services.AddScoped<IEasyAuthProvider>(sp => sp.GetRequiredService<T>());
            var name = at.AuthenticationName;
            builder.Services.AddAuthentication()
                .AddCookie(name, o =>
                {

                    o.LoginPath = "~/account/login";
                });
            return builder;
        }

        public static AuthenticatedEAVFrameworkBuilder AddAuthenticationProvider<T, TOptions,TDep>(
           this AuthenticatedEAVFrameworkBuilder builder,
           Action<TOptions,TDep> configureOptions) where T : class, IEasyAuthProvider
           where TOptions : class
           where TDep:class
        {
            
            builder.Services.AddOptions<TOptions>().Configure(configureOptions);
             
            return builder.AddAuthenticationProvider<T>();
        }

        public static AuthenticatedEAVFrameworkBuilder AddAuthenticationProvider<T, TOptions, TDep1, TDep2>(
          this AuthenticatedEAVFrameworkBuilder builder,
          Action<TOptions, TDep1,TDep2> configureOptions) where T : class, IEasyAuthProvider
          where TOptions : class
          where TDep1 : class
          where TDep2 : class
        {

            builder.Services.AddOptions<TOptions>().Configure(configureOptions);

            return builder.AddAuthenticationProvider<T>();
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
