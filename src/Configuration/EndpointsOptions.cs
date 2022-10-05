using Microsoft.AspNetCore.Builder;
using System;

namespace EAVFramework.Configuration
{
    /// <summary>
    /// Configures which endpoints are enabled or disabled.
    /// </summary>
    public class EndpointsOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the QueryRecords endpoint is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the authorize endpoint is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool EnableQueryRecordsEndpoint { get; set; } = true;

        public Action<IEndpointConventionBuilder> EndpointConfiguration { get; set; }

        public Action<IEndpointConventionBuilder> EndpointAuthorizationConfiguration { get; set; } = DefaultEndpointAuthorizationConfiguration;

        private static void DefaultEndpointAuthorizationConfiguration(IEndpointConventionBuilder obj)
        {
            obj.RequireAuthorization("EAVAuthorizationPolicy");
        }

        public EndpointsOptions AllowAnonymous()
        {
            EndpointAuthorizationConfiguration = null;
            return this;
        }
    }
}
