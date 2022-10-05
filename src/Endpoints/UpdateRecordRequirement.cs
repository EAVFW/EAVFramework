using EAVFramework.Validation;
using Microsoft.AspNetCore.Authorization;

namespace EAVFramework.Endpoints
{
    public class UpdateRecordRequirement : IAuthorizationRequirement, IAuthorizationRequirementError
    {
        public UpdateRecordRequirement(string entityName)
        {
            EntityName = entityName;
        }

        public string EntityName { get; }

        public ValidationError ToError()
        {
            return new ValidationError
            {
                Error = "No permission to update record",
                Code = "NO_UPDATE_PERMISSION",
                ErrorArgs = new[]
                {
                    EntityName
                },
                EntityCollectionSchemaName=EntityName

            };
        }
    }
}
