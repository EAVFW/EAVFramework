using EAVFramework.Validation;
using Microsoft.AspNetCore.Authorization;
using System;

namespace EAVFramework.Endpoints
{
    public class UpdateRecordRequirement : IAuthorizationRequirement, IAuthorizationRequirementError
    {
        public UpdateRecordRequirement(string entityName, Type context)
        {
            EntityName = entityName;
            Context = context;
        }

         
        public string EntityName { get; }
        public Type Context { get; }
        public Type Type { get; }

        public AuthorizationError ToError()
        {
            return new AuthorizationError
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
