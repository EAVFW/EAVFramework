using DotNetDevOps.Extensions.EAVFramework.Validation;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public interface IAuthorizationRequirementError
    {
        ValidationError ToError();
    }
}
