using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    internal class DeleteRecordEndpoint<TContext> : IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly EAVDBContext<TContext> _context;
        private readonly IConfiguration configuration;
        private readonly ILogger<DeleteRecordEndpoint<TContext>> _logger;

        public DeleteRecordEndpoint(
           EAVDBContext<TContext> context,
            
            IConfiguration configuration,
          
            ILogger<DeleteRecordEndpoint<TContext>> logger)  
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;

            //var record = await _context.ReadRecordAsync(context, new ReadOptions { LogPayload = configuration.GetValue<bool>($"EAVFramework:DeleteRecordEndpoint:LogPayload", false) });

            var record = await _context.DeleteAsync(entityName, Guid.Parse(recordId));
           
            var _operation = await _context.SaveChangesAsync(context.User);

            if (_operation.Errors.Any())
                return new DataValidationErrorResult(new { errors = _operation.Errors });


            return new DataEndpointResult(new { id = record.CurrentValues.GetValue<Guid>("Id") });

        }

      
    }
}
