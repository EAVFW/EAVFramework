using Aspire.Hosting.ApplicationModel;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public record EAVFWModelMetadataAnnotation(string ModelPath) : IResourceAnnotation
    {
    }

}