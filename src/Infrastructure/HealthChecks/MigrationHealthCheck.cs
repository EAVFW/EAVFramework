using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Infrastructure.HealthChecks
{
   

    public class MigrationHealthCheck<T> : IHealthCheck
       where T : DbContext
    {
        private static readonly Random _rnd = new Random();
        private readonly IServiceProvider _serviceProvider;

        public MigrationHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {


            try
            {
                var dbContext = _serviceProvider.GetRequiredService<T>();
                var migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
                var sql = migrator.GenerateScript();
                var pending = dbContext.Database.GetPendingMigrations();
                if (pending.Any())
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("missing migrations", data: new Dictionary<string, object> { ["pending"] = pending }));
                }

            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(ex.Message, ex));
            }

            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
