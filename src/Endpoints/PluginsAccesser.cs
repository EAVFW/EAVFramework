using DotNetDevOps.Extensions.EAVFramework.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class PluginsAccesser : IEnumerable<EntityPlugin>
    {
        IEnumerable<EntityPlugin> _plugins { get; }
        public PluginsAccesser(IEnumerable<EntityPlugin> plugins)
        {
            _plugins = plugins.OrderBy(x => x.Order).ToArray();
        }

        public IEnumerator<EntityPlugin> GetEnumerator()
        {
            return _plugins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _plugins.GetEnumerator();
        }
    }
}
