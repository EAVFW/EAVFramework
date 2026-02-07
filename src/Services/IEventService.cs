using EAVFramework.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Services
{

    public interface IContextInitializer
    {
        System.Threading.Tasks.Task InitializeContextAsync();
    }
    public class DefaultContextInitializer : IContextInitializer
    {
        public System.Threading.Tasks.Task InitializeContextAsync()
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }


    /// <summary>
    /// Interface for the event service
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Raises the specified event.
        /// </summary>
        /// <param name="evt">The event.</param>
        Task RaiseAsync(Event evt);

        /// <summary>
        /// Indicates if the type of event will be persisted.
        /// </summary>
        bool CanRaiseEventType(EventTypes evtType);
    }

    /// <summary>
    /// Models persistence of events
    /// </summary>
    public interface IEventSink
    {
        /// <summary>
        /// Raises the specified event.
        /// </summary>
        /// <param name="evt">The event.</param>
        Task PersistAsync(Event evt);
    }
}
