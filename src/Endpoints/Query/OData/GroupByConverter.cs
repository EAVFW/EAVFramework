using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EAVFramework.Endpoints.Query.OData
{
    /// <summary>
    /// Singleton - return the Values Property from type
    /// </summary>
    public class GroupByConverter : IODataConverter
    {
        private readonly IODataConverterFactory odatatConverterFactory;

        public PropertyInfo Method { get; }

        public GroupByConverter(Type type, IODataConverterFactory odatatConverterFactory)
        {
            Method = type.GetProperty("Values");
            this.odatatConverterFactory = odatatConverterFactory;
        }

        public object Convert(object data)
        {
            var poco = Method.GetValue(data) as Dictionary<string, object>;

            // poco["$type"] = typeParser.GetDataType(data);
            foreach (var kv in poco.ToArray())
            {
                if (kv.Value == null)
                {
                    poco.Remove(kv.Key);
                    continue;
                }

                var converter = odatatConverterFactory.CreateConverter(kv.Value.GetType());
                var value = converter.Convert(kv.Value);
                if (value == null)
                {
                    poco.Remove(kv.Key);
                }
                else
                {
                    poco[kv.Key] = value;
                }
            }

            return poco;
        }
    }
}
