using Aspire.Hosting.ApplicationModel;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    /// <summary>
    /// The aspire annotation that is used to wait for a resource to be in a specific state before starting.
    /// </summary>
    /// <param name="resource"></param>

    public class NeedsAnnotation(IResource resource) : IResourceAnnotation
    {
        /// <summary>
        /// The resource that is being waited on
        /// </summary>
        public IResource Resource { get; } = resource;

        /// <summary>
        /// The states to wait for. If null, it will wait for the resource to be in the "Running" state.
        /// </summary>
        public string[]? States { get; set; }
        
        /// <summary>
        /// Indicates if it should wait until the resource has run to completion.
        /// </summary>

        public bool WaitUntilCompleted { get; set; }
    }

   
}
