using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    /// <summary>
    /// An annotation that associates a health check factory with a resource
    /// </summary>
    /// <param name="healthCheckFactory">A function that creates the health check</param>
    public class HealthCheckAnnotation(Func<IResource, CancellationToken, Task<IHealthCheck?>> healthCheckFactory) : IResourceAnnotation
    {
        public Func<IResource, CancellationToken, Task<IHealthCheck?>> HealthCheckFactory { get; } = healthCheckFactory;

        public static HealthCheckAnnotation Create(Func<EAVFWModelProjectResource, IHealthCheck> connectionStringFactory)
        {
            return new(async (resource, token) =>
            {
                if (resource is EAVFWModelProjectResource eavmodel)
                {
                    return connectionStringFactory(eavmodel);
                }

                return null;

            });
        }
    }

}