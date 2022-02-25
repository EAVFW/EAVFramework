using DotNetDevOps.Extensions.EAVFramework.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class PluginsAccesser<TContext> : IEnumerable<EntityPlugin>
        where TContext: DynamicContext
    {
        IEnumerable<EntityPlugin> _plugins { get; }
        public PluginsAccesser(IEnumerable<EntityPlugin> plugins, TContext context)
        {
            _plugins = plugins.OrderBy(x => x.Order)
                .Where(x=>context.IsPluginEnabled( x.Handler))               
                .ToArray();
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
