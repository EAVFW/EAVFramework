using System.Collections;
using System.Collections.Generic;

namespace EAVFramework.Endpoints.Query.OData
{
    internal class EnumerableConverter : IODataConverter
    {

        private IODataConverterFactory odatatConverterFactory;

        public EnumerableConverter(IODataConverterFactory odatatConverterFactory)
        {

            this.odatatConverterFactory = odatatConverterFactory;
        }

        public object Convert(object data)
        {
            if (data is byte[])
                return data;

            var list = new List<object>();
            foreach (var i in data as IEnumerable)
            {
                var converter = odatatConverterFactory.CreateConverter(i.GetType());
                list.Add(converter.Convert(i));
            }
            return list;
        }
    }
}
