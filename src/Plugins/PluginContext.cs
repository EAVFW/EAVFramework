using System.Collections.Concurrent;
using EAVFramework.Validation;

namespace EAVFramework.Plugins
{
    public class PluginContext
    {
        public ConcurrentBag<CoreError> Errors { get; } = new ConcurrentBag<CoreError>();
    }
}