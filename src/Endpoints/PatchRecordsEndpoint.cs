using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class PatchRecordsEndpoint<TContext> : IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly TContext _context;

        public PatchRecordsEndpoint(TContext context)
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;

            var trackedEntity = _context.Update(entityName, await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream()))));
            await _context.SaveChangesAsync();



            return new DataEndpointResult(new { id = trackedEntity.CurrentValues.GetValue<Guid>("Id") });


        }
    }
}
