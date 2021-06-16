using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
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
        Retrieve,
        RetrieveAll
    }
    public enum EntityPluginMode
    {
        Sync,
        Async
    }

    public interface IPluginScheduler
    {
        Task ScheduleAsync(EntityPlugin plugin, object entity);
    }
}
