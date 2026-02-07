using Microsoft.AspNetCore.Builder;
using System;

namespace EAVFramework.Configuration
{
    public class PatchEndpointOptions
    {
        public bool AllowUnknownData { get; set; } = true;
        public bool LogWarningForUnknownData { get; set; } = true;
    }
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

        public PatchEndpointOptions PatchEndpointOptions { get; set; } = new PatchEndpointOptions();

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
