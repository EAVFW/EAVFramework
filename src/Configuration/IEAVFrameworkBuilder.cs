using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace DotNetDevOps.Extensions.EAVFramework.Configuration
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
