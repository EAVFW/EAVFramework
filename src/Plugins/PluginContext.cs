using System.Collections.Concurrent;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public class PluginContext
    {
        public ConcurrentBag<ValidationError> Errors { get; } = new ConcurrentBag<ValidationError>();
    }
}
