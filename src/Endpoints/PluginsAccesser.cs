﻿using EAVFramework.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace EAVFramework.Endpoints
{
    public class PluginsAccesser<TContext> : IEnumerable<EntityPlugin>
        where TContext: DynamicContext
    {
        IEnumerable<EntityPlugin> _plugins { get; }
        public PluginsAccesser(IEnumerable<EntityPlugin> plugins, TContext context)
        {
            _plugins = plugins.OrderBy(x => x.Order)
                .Where(x=>x is EntityPlugin<TContext> &&  context.IsPluginEnabled( x.Handler))               
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
