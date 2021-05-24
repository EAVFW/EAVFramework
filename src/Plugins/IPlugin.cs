using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public interface IPlugin
    {

    }
    public interface IPlugin<TContext,T> : IPlugin
        where TContext : DynamicContext
        where T : DynamicEntity
    {
        Task Execute(PluginContext<TContext, T> context);
    }
}
