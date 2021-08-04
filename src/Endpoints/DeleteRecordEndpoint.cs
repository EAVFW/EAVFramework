using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    internal class DeleteRecordEndpoint<TContext> : BaseEndpoint, IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly TContext _context;

        private readonly ILogger<DeleteRecordEndpoint<TContext>> _logger;

        public DeleteRecordEndpoint(
            TContext context,
            IEnumerable<EntityPlugin> plugins,
            IPluginScheduler pluginScheduler,
            ILogger<DeleteRecordEndpoint<TContext>> logger) : base(plugins, EntityPluginOperation.Delete, pluginScheduler)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;

            var strategy = _context.Database.CreateExecutionStrategy();


            var _operation = await strategy.ExecuteAsync(async () =>
            {
                using var scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

                var operation = new OperationContext<TContext>
                {
                    Context = _context
                };

                var record = await _context.FindAsync(entityName, Guid.Parse(recordId));
                operation.Entity = _context.Entry(record);

                //foreach (var navigation in operation.Entity.Collections)
                //{
                //    await navigation.LoadAsync();
                //}

                operation.Entity.State = EntityState.Deleted;
                 
                operation.Errors = await RunPreValidation(scope.ServiceProvider, context, operation.Entity);

                if (operation.Errors.Any())
                    return operation;

                await RunPipelineAsync(scope.ServiceProvider,context, operation);

                return operation;

            });


         

            await RunAsyncPostOperation(context, _operation.Entity);


            return new DataEndpointResult(new { id = _operation.Entity.CurrentValues.GetValue<Guid>("Id") });

        }

      
    }
}
