using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Plugins
{
    public interface IPlugin
    {

    }
    public interface IPlugin<TContext> : IPlugin 
        where TContext : DynamicContext
    {

    }
    public interface IPlugin<TContext,T> : IPlugin<TContext>
        where TContext : DynamicContext
        where T : DynamicEntity
    {
        Task Execute(PluginContext<TContext, T> context);
    }
}
