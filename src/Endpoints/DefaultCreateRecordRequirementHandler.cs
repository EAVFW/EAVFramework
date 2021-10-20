using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class DefaultCreateRecordRequirementHandler :
    AuthorizationHandler<CreateRecordRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, CreateRecordRequirement requirement)
        {
           
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
