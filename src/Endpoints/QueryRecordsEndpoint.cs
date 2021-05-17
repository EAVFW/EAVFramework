using DotNetDevOps.Extensions.EAVFramwork.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramwork.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramwork.Constants;

namespace DotNetDevOps.Extensions.EAVFramwork.Endpoints
{
    public class QueryRecordsEndpoint<TContext> : IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly TContext _context;

        public QueryRecordsEndpoint(TContext context)
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var result = await _context.Set(routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string, context.Request);

            return new DataEndpointResult(new { items = result.Items.ToArray() });


        }
    }

    public class RetrieveRecordsEndpoint<TContext> : IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly TContext _context;

        public RetrieveRecordsEndpoint(TContext context)
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
            context.Request.QueryString.Add("$filter", $"id eq {recordId}");

            var query = await _context.Set(entityName, context.Request);

            return new DataEndpointResult(new { value = query.Items.First() });


        }
    }

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
