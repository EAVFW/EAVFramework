using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        public PatchRecordsEndpoint(TContext context, IEnumerable<EntityPlugin> plugins) :base(plugins.Where(c=>c.Operation== EntityPluginOperation.Update))
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;

            using var transaction = _context.Database.BeginTransaction();

            var trackedEntity = _context.Update(entityName, await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream()))));

         

            List<ValidationError> errors = await RunPreValidation(context, trackedEntity);

            if (errors.Any())
                return new DataValidationErrorResult(new { errors = errors });

            await RunPreOperation(context, trackedEntity);

            await _context.SaveChangesAsync();

            await RunPostOperation(context, trackedEntity);

            transaction.Commit();



            return new DataEndpointResult(new { id = trackedEntity.CurrentValues.GetValue<Guid>("Id") });


        }
    }
}
