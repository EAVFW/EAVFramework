using EAVFramework.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;
using Microsoft.Extensions.Options;

namespace EAVFramework.Endpoints
{
    public class PluginsAccesser<TContext> : IEnumerable<EntityPlugin>
        where TContext: DynamicContext
    {
        private readonly IOptions<DynamicContextOptions> modelOptions;

        IEnumerable<EntityPlugin> _plugins { get; }
        public PluginsAccesser(IEnumerable<EntityPlugin> plugins, IOptions<DynamicContextOptions> modelOptions)
        {
            this.modelOptions = modelOptions;

            _plugins = plugins.OrderBy(x => x.Order)
                .Where(x=>x is EntityPlugin<TContext> &&  IsPluginEnabled( x.Handler))               
                .ToArray();
           
        }
        internal bool IsPluginEnabled(Type handler)
        {
            return !modelOptions.Value.DisabledPlugins?.Contains(handler) ?? true;

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
