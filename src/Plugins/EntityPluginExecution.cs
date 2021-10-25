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
        Delete,
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
        Task ScheduleAsync(EntityPlugin plugin, string identityid, object entity);
    }
}
