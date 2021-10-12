using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using DotNetDevOps.Extensions.EAVFramework.Validation;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class OperationContext<TContext>
    {
        public TContext Context { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
     //   public EntityEntry Entity { get; set; }
    }
}
