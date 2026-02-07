using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public class AspireEAVFWHealthCheck : IHealthCheck
    {
        private EAVFWModelProjectResource _modelResource;

        public AspireEAVFWHealthCheck(EAVFWModelProjectResource modelResource)
        {
            _modelResource = modelResource;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var targetDatabaseResource = _modelResource.Annotations.OfType<TargetDatabaseResourceAnnotation>().Single().TargetDatabaseResource;
            var targetSQLServerResource = targetDatabaseResource.Parent;
            var serverConnectionString = await targetSQLServerResource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
            var connectionString = await targetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);


            return HealthCheckResult.Healthy();
        }
    }

}