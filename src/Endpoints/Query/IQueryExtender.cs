using System.Linq;

namespace EAVFramework.Endpoints.Query
{
    public interface IQueryExtender<TContext> where TContext : DynamicContext
    {
        IQueryable ApplyTo(IQueryable metadataQuerySet, QueryContext<TContext> context);
    }
}
