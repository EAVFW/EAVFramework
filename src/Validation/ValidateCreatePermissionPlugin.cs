using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Authorization;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public class ValidateCreatePermissionPlugin : IPlugin<DynamicContext, DynamicEntity>
    {
         
        private readonly IAuthorizationService _authorizationService;

        public ValidateCreatePermissionPlugin(IPermissionStore permissionStore, IAuthorizationService authorizationService)
        {
        
            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }
        public async Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {
        

            var auth = await _authorizationService.AuthorizeAsync(context.User, context.EntityResource, new CreateRecordRequirement(context.EntityResource.EntityCollectionSchemaName));

            if (!auth.Succeeded)
            {
                foreach (var err in auth.Failure.FailedRequirements.OfType<IAuthorizationRequirementError>()) {
                    context.AddValidationError(err.ToError());
                        
                        };
              
            }
        }
    }

    public class ValidateUpdatePermissionPlugin : IPlugin<DynamicContext, DynamicEntity>
    {

        private readonly IAuthorizationService _authorizationService;

        public ValidateUpdatePermissionPlugin(IPermissionStore permissionStore, IAuthorizationService authorizationService)
        {

            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }
        public async Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {


            var auth = await _authorizationService.AuthorizeAsync(context.User, context.EntityResource, new UpdateRecordRequirement(context.EntityResource.EntityCollectionSchemaName));

            if (!auth.Succeeded)
            {
                foreach (var err in auth.Failure.FailedRequirements.OfType<IAuthorizationRequirementError>())
                {
                    context.AddValidationError(err.ToError());

                };

            }
        }
    }
}