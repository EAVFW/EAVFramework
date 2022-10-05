using EAVFramework.Plugins;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace EAVFramework.Configuration
{
    /// <summary>
    /// IdentityServer builder Interface
    /// </summary>
    public interface IEAVFrameworkBuilder
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        IServiceCollection Services { get; }
    }
}
