using DotNetDevOps.Extensions.EAVFramework.Configuration;
using DotNetDevOps.Extensions.EAVFramework.Hosting;

namespace DotNetDevOps.Extensions.EAVFramework.Extensions
{
    internal static class EndpointOptionsExtensions
    {
        public static bool IsEndpointEnabled(this EndpointsOptions options, Endpoint endpoint)
        {
            return endpoint?.Name switch
            {
                Constants.EndpointNames.QueryRecords => options.EnableQueryRecordsEndpoint,
              
                _ => true
            };
        }
    }
}
