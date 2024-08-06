using Microsoft.AspNetCore.OData.Query.Container;
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
        
        public ConvertResult Convert(object data)
        {
            if (data is byte[])
               return new ConvertResult { Value = data };

            var list = new List<object>();
            foreach (var i in data as IEnumerable)
            {
                var converter = odatatConverterFactory.CreateConverter(i.GetType());
                var item = converter.Convert(i);
                list.Add(item.Value);
            }
            
            var result = new ConvertResult { Value = list };
            if (data is ITruncatedCollection collection)
            {
                result.PageSize = collection.PageSize;
                result.HasMore = collection.IsTruncated;
                
                result.TotalCount = data.GetType().GetProperty("TotalCount").GetValue(data) as long?;
            }
            
            return result;
        }
    }
}
