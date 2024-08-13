using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting;
using System.Text;
using static EAVFramework.Extensions.Aspire.Hosting.AspireBuilderExtensions;

namespace EAVFramework.Extensions.Aspire.Hosting
{


    public static class AspireNeedsBuilderExtensions
    {

        public static IResourceBuilder<T> Needs<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> other, params string[]  states)
            where T : IResource
        {
            builder.ApplicationBuilder.AddDependencies();
            return builder.WithAnnotation(new NeedsAnnotation(other.Resource) {  States=states});
        }

        /// <summary>
        /// Mark the given resource as a dependency for the current resource. 
        /// The current resource will wait for the dependency to be in the "Running" state before starting.
        /// 
        /// <see cref="NeedsAnnotation"/> for adding a annotation manually.
        /// </summary>
        /// <typeparam name="T">The resource type.</typeparam>
        /// <param name="builder">The resource builder.</param>
        /// <param name="other">The resource to wait for.</param>
        public static IResourceBuilder<T> Needs<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> other)
            where T : IResource
        {
            builder.ApplicationBuilder.AddDependencies();
            return builder.WithAnnotation(new NeedsAnnotation(other.Resource));
        }

        /// <summary>
        /// Mark the given resource as a dependency for the current resource. 
        /// The current resource will wait until the dependency has run to completion before starting.
        /// </summary>
        /// <typeparam name="T">The resource type.</typeparam>
        /// <param name="builder">The resource builder.</param>
        /// <param name="other">The resource to wait for.</param>
        public static IResourceBuilder<T> Needs<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> other, bool waitUntilCompleted)
            where T : IResource
        {
            builder.ApplicationBuilder.AddDependencies();
            return builder.WithAnnotation(new NeedsAnnotation(other.Resource) { WaitUntilCompleted = waitUntilCompleted });
        }

        /// <summary>
        /// Adds a lifecycle hook that waits for all dependencies to be "running" before starting resources. If that resource
        /// has a health check, it will be executed before the resource is considered "running".
        /// </summary>
        /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
        private static IDistributedApplicationBuilder AddDependencies(this IDistributedApplicationBuilder builder)
        {
            builder.Services.TryAddLifecycleHook<NeedsLifecycleHook>();
            return builder;
        }

       

      
    }

   
}
