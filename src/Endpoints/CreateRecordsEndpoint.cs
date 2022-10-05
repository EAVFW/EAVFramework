using EAVFramework.Endpoints.Results;
using EAVFramework.Hosting;
using EAVFramework.Plugins;
using EAVFramework.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using static EAVFramework.Constants;

namespace EAVFramework.Endpoints
{

    internal class CreateRecordsEndpoint<TContext>: IEndpointHandler<TContext>
        where TContext : DynamicContext
    {
        private readonly EAVDBContext<TContext> _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CreateRecordsEndpoint<TContext>> _logger;
        private readonly IAuthorizationService _authorizationService;

        public CreateRecordsEndpoint(
            EAVDBContext<TContext> context,         
            IConfiguration configuration,
            ILogger<CreateRecordsEndpoint<TContext>> logger,
            IAuthorizationService authorizationService) 
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
          


            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;

            var auth = await _authorizationService.AuthorizeAsync(context.User, _context.CreateEAVResource(entityName,context), new CreateRecordRequirement(entityName));

            if (!auth.Succeeded)
            {
                return new AuthorizationEndpointResult(new { errors = auth.Failure.FailedRequirements.OfType<IAuthorizationRequirementError>().Select(c => c.ToError()) });
            }


            JToken record = await _context.ReadRecordAsync(context, new ReadOptions { LogPayload = _configuration.GetValue<bool>($"EAVFramework:CreateRecordsEndpoint:LogPayload", false) });


            if (context.Request.Headers.ContainsKey("x-batch"))
            {

            
            
            }


            var entity = _context.Add(entityName, record); ;

            var _operation = await _context.SaveChangesAsync(context.User);
             
            if (_operation.Errors.Any())
                return new DataValidationErrorResult(new { errors = _operation.Errors });
             

            return new DataEndpointResult(new { id = entity.CurrentValues.GetValue<Guid>("Id") });





        }


    }
}
