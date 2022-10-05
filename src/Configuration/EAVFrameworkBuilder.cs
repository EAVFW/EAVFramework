using Microsoft.Extensions.DependencyInjection;
using System;

namespace EAVFramework.Configuration
{
    /// <summary>
    /// IdentityServer helper class for DI configuration
    /// </summary>
    public class EAVFrameworkBuilder : IEAVFrameworkBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public EAVFrameworkBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
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
