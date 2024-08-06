using Aspire.Hosting.ApplicationModel;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public record TargetDatabaseResourceAnnotation(string TargetDatabaseResourceName, SqlServerDatabaseResource TargetDatabaseResource) : IResourceAnnotation
    {
    }

}