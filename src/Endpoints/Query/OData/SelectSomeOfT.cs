using EAVFramework.Shared;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace EAVFramework.Endpoints.Query.OData
{
    public class SelectSomeOfT : IODataRuntimeType
    {
        private IODataRuntimeTypeFactory oDataRuntimeTypeFactory;


        private static PropertyInfo untypedInstance = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
            .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper")?.GetProperty("UntypedInstance");
        private static PropertyInfo container = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
            .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper")?.GetProperty("Container");

        private static MethodInfo getElementType = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
           .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper")?.GetMethod("GetElementType", BindingFlags.NonPublic | BindingFlags.Instance);

        public SelectSomeOfT(IODataRuntimeTypeFactory oDataRuntimeTypeFactory)
        {
            this.oDataRuntimeTypeFactory = oDataRuntimeTypeFactory;

        }
        private ConcurrentDictionary<Type, PropertyInfo> _NamedPropertyBag = new ConcurrentDictionary<Type, PropertyInfo>();
        private ConcurrentDictionary<string, string> LogicalNameMapping = new ConcurrentDictionary<string, string>();

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public string GetDataType(object data)
        {
            var test = data as IEdmObject;
            var modeltype = test.GetEdmType();
            var serializedType = LogicalNameMapping.GetOrAdd(modeltype.Definition.FullTypeName(), (typename) =>
            {
                return GetType(typename)?.GetCustomAttribute<EntityAttribute>()?.LogicalName ?? typename;

            });

            //var instance = untypedInstance?.GetValue(data);
            //if (instance== null)
            //{
            //    var aa = getElementType.Invoke(data,null);


            //    var c = container.GetValue(data);
            //    var namedProperty = _NamedPropertyBag.GetOrAdd(c.GetType(), (t) => t.GetProperty("Value"));
            //    var n = namedProperty.GetValue(c);
            //    instance=untypedInstance.GetValue(n);
            //}
            //var serializedType = instance?.GetType().GetCustomAttribute<EntityAttribute>()?.LogicalName;
            return serializedType;
        }
    }
}
