﻿using EAVFramework;
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

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DynamicCodeServiceFactory
    {
        public static DynamicCodeService Create(string schema)
        {
            return new DynamicCodeService(CreateOptions(schema));
        }

        public static CodeGenerationOptions CreateOptions(string schema)
        {
            return new CodeGenerationOptions
            {
                //  MigrationName="Initial",
                Schema = schema,
                JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),
                JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                InverseAttributeCtor = typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) }),
                ForeignKeyAttributeCtor = typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),

                EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),
                EntityTypeBuilderType = typeof(EntityTypeBuilder),
                EntityTypeBuilderToTable = Resolve(() => typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }), "EntityTypeBuilderToTable"),
                EntityTypeBuilderHasKey = Resolve(() => typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[]) }), "EntityTypeBuilderHasKey"),
                EntityTypeBuilderPropertyMethod = Resolve(() => typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), 0, new[] { typeof(string) }), "EntityTypeBuilderPropertyMethod"),

                IsRequiredMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                   .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRequired)), "IsRequiredMethod"),
                IsRowVersionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                     .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRowVersion)), "IsRowVersionMethod"),
                HasConversionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                              .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasConversion), 1, new Type[] { }), "HasConversionMethod"),
                HasPrecisionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                   .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasPrecision), new Type[] { typeof(int), typeof(int) }), "HasPrecisionMethod"),



                DynamicTableType = typeof(IDynamicTable),
                DynamicTableArrayType = typeof(IDynamicTable[]),


                ColumnsBuilderType = typeof(ColumnsBuilder),
                CreateTableBuilderType = typeof(CreateTableBuilder<>),
                CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),
                ColumnsBuilderColumnMethod = Resolve(() => typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance), "ColumnsBuilderColumnMethod"),
                OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),


                MigrationBuilderDropTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)), "MigrationBuilderDropTable"),
                MigrationBuilderCreateTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)), "MigrationBuilderCreateTable"),
                MigrationBuilderSQL = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.Sql)), "MigrationBuilderSQL"),
                MigrationBuilderCreateIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex), new Type[] { typeof(string), typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string) }), "MigrationBuilderCreateIndex"),
                MigrationBuilderDropIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)), "MigrationBuilderDropIndex"),
                MigrationsBuilderAddColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddColumn)), "MigrationsBuilderAddColumn"),
                MigrationsBuilderAddForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddForeignKey), new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(ReferentialAction), typeof(ReferentialAction) }), "MigrationsBuilderAddForeignKey"),
                MigrationsBuilderAlterColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AlterColumn)), "MigrationsBuilderAlterColumn"),
                MigrationsBuilderDropForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropForeignKey)), "MigrationsBuilderDropForeignKey"),

                ReferentialActionType = typeof(ReferentialAction),
                ReferentialActionNoAction = (int)ReferentialAction.NoAction,


                LambdaBase = Resolve(() => typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null), "LambdaBase"),

            };
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
            services.TryAddSingleton<DynamicCodeService>();
            services.AddSingleton(DynamicCodeServiceFactory.CreateOptions(schema));
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
        public static IEAVFrameworkBuilder AddEndpoint<T,TContext>(this IEAVFrameworkBuilder builder, string name, string pattern, params string[] methods)
            where T : class, IEndpointHandler<TContext>
            where TContext : DynamicContext
        {
            builder.Services.AddTransient<T>();
            builder.Services.AddSingleton(new EAVFramework.Hosting.Endpoint<TContext>(name, pattern,methods, typeof(T)));

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

                    o.LoginPath = "/account/login";
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
