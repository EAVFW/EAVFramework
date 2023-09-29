using EAVFramework.Shared;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace EAVFramework.Endpoints.Query.OData
{
    public class ODataRuntimeTypeFactory : IODataRuntimeTypeFactory
    {
        private static ConcurrentDictionary<Type, IODataRuntimeType> _typeParsers = new ConcurrentDictionary<Type, IODataRuntimeType>();
        private static Type selectexpandwrapper = typeof(Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper)
            .Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper`1");


        public IODataRuntimeType CreateTypeParser(Type type, object data)
        {
            return _typeParsers.GetOrAdd(type, type => Factory(type, data));

        }

        private IODataRuntimeType Factory(Type type, object data)
        {
            //
            if (type.IsGenericType &&
                selectexpandwrapper.MakeGenericType(type.GenericTypeArguments[0]).IsAssignableFrom(type) &&

                type.GenericTypeArguments[0].GetCustomAttribute<EntityAttribute>() is EntityAttribute attr && !attr.IsBaseClass)
            {
                return new ConstantRuntimeType(attr.LogicalName);
            }

            return new SelectSomeOfT(this);

        }
    }
}
