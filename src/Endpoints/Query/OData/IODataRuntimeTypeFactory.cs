using System;

namespace EAVFramework.Endpoints.Query.OData
{
    public interface IODataRuntimeTypeFactory
    {
        IODataRuntimeType CreateTypeParser(Type type, object data);
    }
}
