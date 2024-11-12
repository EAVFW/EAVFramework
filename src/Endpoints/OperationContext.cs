using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using EAVFramework.Validation;

namespace EAVFramework.Endpoints
{
    public class OperationContext<TContext>
    {
        public TContext Context { get; set; }
        public List<CoreError> Errors { get; set; } = new List<CoreError>();
        public string PreOperationChanges { get;  set; } = string.Empty;

        public string PostOperationChanges { get;  set; } = string.Empty;
        //   public EntityEntry Entity { get; set; } 
    }
}
