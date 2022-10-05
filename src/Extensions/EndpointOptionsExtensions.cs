using EAVFramework.Configuration;
using EAVFramework.Hosting;

namespace EAVFramework.Extensions
{
    internal static class EndpointOptionsExtensions
    {
        public static bool IsEndpointEnabled<TContext>(this EndpointsOptions options, Endpoint<TContext> endpoint) where TContext : DynamicContext
        {
            return endpoint?.Name switch
            {
                Constants.EndpointNames.QueryRecords => options.EnableQueryRecordsEndpoint,
              
                _ => true
            };
        }
    }
}
