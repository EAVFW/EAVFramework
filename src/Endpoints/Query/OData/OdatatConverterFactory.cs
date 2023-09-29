using System;
using System.Collections;
using System.Collections.Concurrent;

namespace EAVFramework.Endpoints.Query.OData
{
    public class OdatatConverterFactory : IODataConverterFactory
    {
        private static ConcurrentDictionary<Type, IODataConverter> _converters = new ConcurrentDictionary<Type, IODataConverter>();

        public IODataConverter CreateConverter(Type type)
        {
            return _converters.GetOrAdd(type, type =>
             {
                 if (type.Name == "SelectAllAndExpand`1")
                 {
                     return new SelectAllAndExpandConverter(type, this);

                 }
                 else if (type.Name == "SelectSome`1" || type.Name == "SelectAll`1" || type.Name == "SelectSomeAndInheritance`1")
                 {
                     return new SelectCoverter(type, this);


                 }
                 else if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
                 {
                     return new EnumerableConverter(this);

                 }
                 else if (type.Name == "NoGroupByAggregationWrapper" || type.Name == "GroupByWrapper" || type.Name == "AggregationWrapper")
                 {
                     return new GroupByConverter(type, this);
                 }
                 else
                 {
                     return new PrimitivConverter();
                 }

             });

        }
    }
}
