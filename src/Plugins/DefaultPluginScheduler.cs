using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public class DefaultPluginScheduler : IPluginScheduler
    {
        public Task ScheduleAsync(EntityPlugin plugin, object entity)
        {
            return Task.CompletedTask;
        }
    }
}
