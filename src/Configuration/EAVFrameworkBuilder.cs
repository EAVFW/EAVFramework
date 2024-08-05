using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.EntityFrameworkCore;
using EAVFramework.OpenTelemetry;
namespace EAVFramework.Configuration
{
    /// <summary>
    /// IdentityServer helper class for DI configuration
    /// </summary>
    public class EAVFrameworkBuilder<TContext> : IEAVFrameworkBuilder
        where TContext : DynamicContext
    {
        private string schema;
        private string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public EAVFrameworkBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));


        }

        public EAVFrameworkBuilder(IServiceCollection services, string schema, string connectionString) : this(services)
        {
            this.schema = schema;
            this.connectionString = connectionString;
        }

        public void WithDBContext()
        {
            Services.AddDbContext<TContext>((sp, optionsBuilder) =>
            {
                optionsBuilder.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__MigrationsHistory", schema));
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();

                optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
                optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicContextModelCacheKeyFactory>();

            });

        }

        public IEAVFrameworkBuilder WithMetrics()
        {
 
            Services.AddSingleton<EAVMetrics>();


            return this;

        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        public IServiceCollection Services { get; }
    }

}
