using EAVFramework.Validation;

namespace EAVFramework.Endpoints
{
    public interface IAuthorizationRequirementError
    {
        AuthorizationError ToError();
    }
}