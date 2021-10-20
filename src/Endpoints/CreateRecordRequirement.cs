using DotNetDevOps.Extensions.EAVFramework.Validation;
using Microsoft.AspNetCore.Authorization;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class CreateRecordRequirement : IAuthorizationRequirement, IAuthorizationRequirementError
    {
        public CreateRecordRequirement(string entityName)
        {
            EntityCollectionSchemaName = entityName;
        }

        public string EntityCollectionSchemaName { get; }

        public ValidationError ToError()
        {
            return new ValidationError
            {
                Error = "No permission to create record",
                Code = "NO_CREATE_PERMISSION",
                ErrorArgs = new[]
                {
                    EntityCollectionSchemaName
                },
                EntityCollectionSchemaName = EntityCollectionSchemaName

            };
        }
    }
}
