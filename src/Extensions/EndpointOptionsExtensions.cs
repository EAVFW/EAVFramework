using DotNetDevOps.Extensions.EAVFramwork.Configuration;
using DotNetDevOps.Extensions.EAVFramwork.Hosting;

namespace DotNetDevOps.Extensions.EAVFramwork.Extensions
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
