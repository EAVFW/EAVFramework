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
    public static class IEAVFrameworkBuilderExtensions
    {
        public static IEAVFrameworkBuilder WithPluginsDiscovery<T>(this IEAVFrameworkBuilder builder)
        {
            var autoPlugins = typeof(T).Assembly.GetTypes().Where(type => type.GetCustomAttributes<PluginRegistrationAttribute>().Any())
                .ToArray();

            foreach (var plugin in autoPlugins)
            {
                builder.AddPlugin(plugin);
            }

            return builder;
        }
    }
}
