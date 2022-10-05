using System;
using System.Linq;
using System.Threading.Tasks;
using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using Microsoft.AspNetCore.Authorization;

namespace EAVFramework.Validation
{
    public class ValidateCreatePermissionPlugin<TContext> : IPlugin<TContext, DynamicEntity> where TContext : DynamicContext
    {
         
        private readonly IAuthorizationService _authorizationService;

        public ValidateCreatePermissionPlugin(IPermissionStore<TContext> permissionStore, IAuthorizationService authorizationService)
        {
        
            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }
        public async Task Execute(PluginContext<TContext, DynamicEntity> context)
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

    public class ValidateUpdatePermissionPlugin<TContext> : IPlugin<TContext, DynamicEntity> where TContext:DynamicContext
    {

        private readonly IAuthorizationService _authorizationService;

        public ValidateUpdatePermissionPlugin(IPermissionStore<TContext> permissionStore, IAuthorizationService authorizationService)
        {

            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }
        public async Task Execute(PluginContext<TContext, DynamicEntity> context)
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