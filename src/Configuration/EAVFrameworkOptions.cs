using EAVFramework.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EAVFramework.Configuration
{



    public class EAVFrameworkOptions
    {

        

        /// <summary>
        /// Gets or sets the events options.
        /// </summary>
        /// <value>
        /// The events options.
        /// </value>
        public EventsOptions Events { get; set; } = new EventsOptions();


        /// <summary>
        /// Gets or sets the endpoint configuration.
        /// </summary>
        /// <value>
        /// The endpoints configuration.
        /// </value>
        public EndpointsOptions Endpoints { get; set; } = new EndpointsOptions();

        public string RoutePrefix { get; set; } = "/api";

        /// <summary>
        /// Gets or sets the authentication options.
        /// </summary>
        /// <value>
        /// The authentication options.
        /// </value>
        public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();

        /// <summary>
        /// Gets or sets the options for the user interaction.
        /// </summary>
        /// <value>
        /// The user interaction options.
        /// </value>
        public UserInteractionOptions UserInteraction { get; set; } = new UserInteractionOptions();
        public string Schema { get;  set; }
        public string ConnectionString { get;  set; }
        public ClaimsPrincipal SystemAdministratorIdentity { get;  set; }

        /// <summary>
        /// The Host (BaseURL) that the application is running on
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// If the application is running behinad a proxy an dsetting a pathbase.
        /// </summary>
        public string PathBase { get; set; }

    }

}
