using Microsoft.Extensions.DependencyInjection;

namespace EAVFW.Extensions.Manifest.SDK
{
    public static class ServiceRegistrationExtension
    {
        public static IServiceCollection AddManifestSDK<TParameterGenerator>(this IServiceCollection services) where TParameterGenerator : class,IParameterGenerator
        {
            services.AddTransient<ISchemaNameManager, DefaultSchemaNameManager>();
            services.AddTransient<IManifestReplacmentRunner, DefaultManifestReplacementRunner>();
            services.AddTransient<IManifestPathExtracter, DefaultManifestPathExtracter>();           
            services.AddTransient<IManifestEnricher, ManifestEnricher>();
            services.AddTransient<IManifestPermissionGenerator, ManifestPermissionGenerator>();
            services.AddSingleton<IParameterGenerator, TParameterGenerator>();
            services.AddOptions<ManifestEnricherOptions>();

            return services;
        }
    }
}