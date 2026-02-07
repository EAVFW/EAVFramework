using EAVFramework.Configuration;
using EAVFramework.Extensions;
using EAVFramework.Logging;
using EAVFramework.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Events
{

    /// <summary>
    /// Indicates if the event is a success or fail event.
    /// </summary>
    public enum EventTypes
    {
        /// <summary>
        /// Success event
        /// </summary>
        Success = 1,

        /// <summary>
        /// Failure event
        /// </summary>
        Failure = 2,

        /// <summary>
        /// Information event
        /// </summary>
        Information = 3,

        /// <summary>
        /// Error event
        /// </summary>
        Error = 4
    }
}
