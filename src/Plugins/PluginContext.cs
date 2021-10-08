using System.Collections.Concurrent;
using DotNetDevOps.Extensions.EAVFramework.Validation;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public class PluginContext
    {
        public ConcurrentBag<ValidationError> Errors { get; } = new ConcurrentBag<ValidationError>();
    }
}
