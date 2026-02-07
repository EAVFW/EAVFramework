using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System;
using System.Linq;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public sealed class EAVFWModelProjectResource(string name) : Resource(name)
    {
      

        public string GetModelPath()
        {
            var projectMetadata = Annotations.OfType<IProjectMetadata>().FirstOrDefault();
            if (projectMetadata != null)
            {
                var projectPath = projectMetadata.ProjectPath;

                return projectPath;
            }

            var metadata = Annotations.OfType<EAVFWModelMetadataAnnotation>().FirstOrDefault();
            if (metadata != null)
            {
                return metadata.ModelPath;
            }

            throw new InvalidOperationException($"Unable to locate SQL Server Database project package for resource {Name}.");
        }
    }

}