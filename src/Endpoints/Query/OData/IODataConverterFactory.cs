using System;

namespace EAVFramework.Endpoints.Query.OData
{
    public interface IODataConverterFactory
    {
        IODataConverter CreateConverter(Type type);
    }
}
