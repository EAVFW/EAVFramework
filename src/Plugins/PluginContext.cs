using System.Collections.Concurrent;
using EAVFramework.Validation;

namespace EAVFramework.Plugins
{
    public class PluginContext
    {
        public ConcurrentBag<ValidationError> Errors { get; } = new ConcurrentBag<ValidationError>();
    }
}
