using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class OperationContext<TContext>
    {
        public TContext Context { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public EntityEntry Entity { get; set; }
    }
}
