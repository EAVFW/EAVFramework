using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using EAVFramework.Validation;

namespace EAVFramework.Endpoints
{
    public class OperationContext<TContext>
    {
        public TContext Context { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public string PreOperationChanges { get;  set; } = string.Empty;

        public string PostOperationChanges { get;  set; } = string.Empty;
        //   public EntityEntry Entity { get; set; } 
    }
}
