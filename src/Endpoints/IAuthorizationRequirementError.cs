using EAVFramework.Validation;

namespace EAVFramework.Endpoints
{
    public interface IAuthorizationRequirementError
    {
        ValidationError ToError();
    }
}
