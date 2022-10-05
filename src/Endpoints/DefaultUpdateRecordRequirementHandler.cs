using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints
{
    public class DefaultUpdateRecordRequirementHandler :
   AuthorizationHandler<UpdateRecordRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, UpdateRecordRequirement requirement)
        {

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
