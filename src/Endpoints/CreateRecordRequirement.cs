using EAVFramework.Validation;
using Microsoft.AspNetCore.Authorization;
using System;

namespace EAVFramework.Endpoints
{
    public class CreateRecordRequirement : IAuthorizationRequirement, IAuthorizationRequirementError
    {
        public CreateRecordRequirement(string entityName, Type context)
        {
            EntityCollectionSchemaName = entityName;
            Context = context;
        }

        public string EntityCollectionSchemaName { get; }
        public Type Context { get; }

        public AuthorizationError ToError()
        {
            return new AuthorizationError
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