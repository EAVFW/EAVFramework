using EAVFramework.Configuration;
using EAVFramework.Shared;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query.Container;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using System;
using System.Linq;
using System.Reflection;

namespace EAVFramework.Endpoints.Query.OData
{
    internal class SelectCoverter : IODataConverter
    {
        private static IODataRuntimeTypeFactory typeParser = new ODataRuntimeTypeFactory();


        private IODataConverterFactory odatatConverterFactory;
        private readonly IOptions<EAVFrameworkOptions> options;
        private Func<IEdmModel, IEdmStructuredType, IPropertyMapper> MapperProvider;

        public SelectCoverter(Type type, IODataConverterFactory odatatConverterFactory, IOptions<EAVFrameworkOptions> options)
        {
            //this.type = type;
            this.odatatConverterFactory = odatatConverterFactory;
            this.options = options;
            //this.entityProperty = type.GetMethod("ToDictionary", new[] { typeof(Func<IEdmModel, IEdmStructuredType, IPropertyMapper>) });
            var SelectExpandWrapperConverter = type.Assembly.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapperConverter");
            MapperProvider = (Func<IEdmModel, IEdmStructuredType, IPropertyMapper>) SelectExpandWrapperConverter.GetField("MapperProvider", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);



        }



        //Microsoft.AspNetCore.OData.Query.Container.NamedProperty<T> //https://github.com/OData/AspNetCoreOData/blob/main/src/Microsoft.AspNetCore.OData/Query/Container/NamedPropertyOfT.cs



        public ConvertResult Convert(object data, QueryContext context)
        {

            //https://github.com/OData/AspNetCoreOData/blob/main/src/Microsoft.AspNetCore.OData/Query/Wrapper/SelectAllOfT.cs
            //Microsoft.AspNetCore.OData.Query.Wrapper.SelectAll<KFST.Vanddata.Model.Identity>

            var poco = (data as Microsoft.AspNetCore.OData.Query.Wrapper.ISelectExpandWrapper).ToDictionary(MapperProvider);

            var typeParser = SelectCoverter.typeParser.CreateTypeParser(data.GetType(), data);
            // var poco = (IDictionary<string, object>)entityProperty.Invoke(data, new object[] { MapperProvider });


            if (options.Value.ODataOptions.UseODataContextCountSerialization)
            {

                poco["@odata.context"] = $"{new Uri(context.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority)}/api/entities/$metadata#{typeParser.GetCollectionSchemaName(data)}/$entity";
            }
            else
            {
                poco["$type"] = typeParser.GetDataType(data);
            }



            foreach (var kv in poco.ToArray())
            {
                if (kv.Value == null)
                {
                    if (options.Value.ODataOptions.RemoveNullsWhenSerialize)
                        poco.Remove(kv.Key);

                    continue;
                }

                var converter = odatatConverterFactory.CreateConverter(kv.Value.GetType());
                var result = converter.Convert(kv.Value, context);
                if (result?.Value == null)
                {
                    poco.Remove(kv.Key);
                }
                else
                {
                    poco[kv.Key] = result.Value;
                    if (result.TotalCount.HasValue)
                    {
                        poco[kv.Key + "@odata.count"] = result.TotalCount;

                    }

                }
            }

            return new ConvertResult { Value = poco };
        }
    }
}