using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using System;

namespace EAVFramework.Extensions.Aspire.Hosting
{

    public static class AspireBuilderExtensions
    {
        /// <summary>
        /// Adds a EAVFW Model Project resource to the application.
        /// </summary>
        /// <typeparam name="TProject">The model project that contains the manifest.g.json file</typeparam>
        /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/> instance to add the model project to.</param>
        /// <param name="name">Name of the resource.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> AddEAVFWModel<TProject>(this IDistributedApplicationBuilder builder, string name)
            where TProject : IProjectMetadata, new()
        {


            var resource = new EAVFWModelProjectResource(name);

            return builder.AddResource(resource)
                 .WithAnnotation(HealthCheckAnnotation.Create(cs => new AspireEAVFWHealthCheck(cs)))
                 .WithAnnotation(new TProject());
        }



        /// <summary>
        /// Adds a EAVFW Model Project resource to the application.
        /// </summary>
        /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/> instance to add the model project to.</param>
        /// <param name="name">Name of the resource.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> AddEAVFWModel(this IDistributedApplicationBuilder builder, string name)
        {
            var resource = new EAVFWModelProjectResource(name);


            return builder.AddResource(resource)
               .WithAnnotation(HealthCheckAnnotation.Create(cs => new AspireEAVFWHealthCheck(cs)));
        }
       

        
        /// <summary>
        /// Restore a database from a remote .bak file
        /// </summary>
        /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the EAVFW Model Resource</param>
        /// <param name="bakpath">Path to the .dacpac file.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> FromBackup(this IResourceBuilder<EAVFWModelProjectResource> builder, string bakpath)
        {
            throw new NotImplementedException("Not done yet");           
        }

        /// <summary>
        /// Publishes the EAVFW Model to the target <see cref="SqlServerDatabaseResource"/>.
        /// </summary>
        /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the EAVFW Model Project that should be published to a target database project</param>
        /// <param name="target">An <see cref="IResourceBuilder{T}"/> representing the target <see cref="SqlServerDatabaseResource"/> to publish the model to.</param>
        /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the resource.</returns>
        public static IResourceBuilder<EAVFWModelProjectResource> PublishTo(
            this IResourceBuilder<EAVFWModelProjectResource> builder, IResourceBuilder<SqlServerDatabaseResource> target)
        {
            builder.ApplicationBuilder.Services.TryAddLifecycleHook<PublishEAVFWProjectLifecycleHook>();
            builder.WithAnnotation(new TargetDatabaseResourceAnnotation(target.Resource.Name,target.Resource), ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }


        /// <summary>
        /// Mark the model project to include the NetTopologySuite library when generating the database schemas
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>

        public static IResourceBuilder<EAVFWModelProjectResource> WithNetTopologySuite(
        this IResourceBuilder<EAVFWModelProjectResource> builder)
        {

            builder.WithAnnotation(new NetTopologySuiteAnnotation(), ResourceAnnotationMutationBehavior.Replace);
            return builder;
        }




    }

}