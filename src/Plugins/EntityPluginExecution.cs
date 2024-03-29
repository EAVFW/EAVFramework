﻿using System.Threading.Tasks;

namespace EAVFramework.Plugins
{
    public enum EntityPluginExecution
    {
        PreValidate,
        PreOperation,
        PostOperation
    }
    public enum EntityPluginOperation
    {
        Create,
        Update,
        Delete,
        Retrieve,
        RetrieveAll
    }
    public enum EntityPluginMode
    {
        Sync,
        Async
    }

    public interface IPluginScheduler<TContext> where TContext : DynamicContext
    {
        Task ScheduleAsync(EntityPlugin plugin, string identityid, object entity);
    }
}
