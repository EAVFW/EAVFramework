using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    internal class PatchRecordsEndpoint<TContext> : BaseEndpoint, IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly TContext _context;

        public PatchRecordsEndpoint(TContext context, IEnumerable<EntityPlugin> plugins, IPluginScheduler pluginScheduler) :base(plugins, EntityPluginOperation.Update, pluginScheduler)
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;

            var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream())));

            var strategy = _context.Database.CreateExecutionStrategy();


            var _operation = await strategy.ExecuteAsync(async () =>
            {
                using var scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

                var operation = new OperationContext<TContext>
                {
                    Context = _context
                };

                operation.Entity = _context.Update(entityName, record);
               

                operation.Errors = await RunPreValidation(scope.ServiceProvider,context, operation.Entity);

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
