using System;
using System.Reflection;

namespace EAVFramework.Endpoints.Query.OData
{
    internal class SelectAllAndExpandConverter : IODataConverter
    {
        private readonly Type type;
        private OdatatConverterFactory odatatConverterFactory;
        private PropertyInfo entityProperty;

        public SelectAllAndExpandConverter(Type type, OdatatConverterFactory odatatConverterFactory)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.odatatConverterFactory = odatatConverterFactory ?? throw new ArgumentNullException(nameof(odatatConverterFactory));
            entityProperty = type.GetProperty("Instance");
        }

        public ConvertResult Convert(object data)
        {
            var value = entityProperty.GetValue(data);

            var converter = odatatConverterFactory.CreateConverter(value.GetType());

            return converter.Convert(value);
        }
    }
}
