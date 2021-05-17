using DotNetDevOps.Extensions.EAVFramwork.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramwork.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramwork.Constants;

namespace DotNetDevOps.Extensions.EAVFramwork.Endpoints
{
    public class CreateRecordsEndpoint<TContext> : IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly TContext _context;
        private readonly ILogger<CreateRecordsEndpoint<TContext>> _logger;

        public CreateRecordsEndpoint(TContext context, ILogger<CreateRecordsEndpoint<TContext>> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;

            var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream())));
            //var validator = context.RequestServices.GetService(typeof(IRecordValidator<>).MakeGenericType(_context.GetRecordType(entityName)));
            //if (validator != null)
            //{
            //    _logger.LogWarning("Validator was registered");
            //}
            var a = _context.Add(entityName, record);
            await _context.SaveChangesAsync();

            return new DataEndpointResult(new { id = a.CurrentValues.GetValue<Guid>("Id") });

            
      


        }
    }
}
