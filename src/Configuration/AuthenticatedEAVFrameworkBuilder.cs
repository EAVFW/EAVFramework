using Microsoft.Extensions.DependencyInjection;

namespace EAVFramework.Configuration
{
    public class AuthenticatedEAVFrameworkBuilder //: EAVFrameworkBuilder
    {
        private readonly IEAVFrameworkBuilder _eavFrameworkBuilder;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public AuthenticatedEAVFrameworkBuilder(IEAVFrameworkBuilder eavFrameworkBuilder)
        {
            _eavFrameworkBuilder = eavFrameworkBuilder;
        }

        public IServiceCollection Services => _eavFrameworkBuilder.Services;
    }

}
