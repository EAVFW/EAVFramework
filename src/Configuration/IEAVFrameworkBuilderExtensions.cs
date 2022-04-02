using DotNetDevOps.Extensions.EAVFramework.Infrastructure.HealthChecks;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace DotNetDevOps.Extensions.EAVFramework.Configuration
{
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

        public static IEAVFrameworkBuilder WithDatabaseHealthCheck<T>(this IEAVFrameworkBuilder builder)
        {
            builder.Services.AddHealthChecks()
              .AddCheck<MigrationHealthCheck<DynamicContext>>(typeof(T).Name+"MigrationCheck");

            return builder;
        }
    }
}
