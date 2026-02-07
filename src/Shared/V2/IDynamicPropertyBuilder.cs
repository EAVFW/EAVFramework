using System;

namespace EAVFramework.Shared.V2
{
    public interface IDynamicPropertyBuilder
    {
        DynamicPropertyBuilder AddProperty(string attributekey, string propertyName, string logicalName, string type);
    }
}
