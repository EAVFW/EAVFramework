using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EAVFramework.Infrastructure
{
    public class NavigationCollectionConverter<T> : JsonConverter<ICollection<T>>
    {
        private readonly JsonSerializerOptions options;

        public NavigationCollectionConverter(JsonSerializerOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options;
        }
         
        public override ICollection<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (ICollection<T>)JsonSerializer.Deserialize( ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, ICollection<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
    }

    public class DictionaryTKeyEnumTValueConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            if (typeToConvert.GetGenericTypeDefinition() != typeof(ICollection<>))
            {
                return false;
            }

            return true;
        }

        public override JsonConverter CreateConverter(
            Type type,
            JsonSerializerOptions options)
        {
            Type keyType = type.GetGenericArguments()[0];
            

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(NavigationCollectionConverter<>).MakeGenericType(
                    new Type[] { keyType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null);

            return converter;
        }
    }
    
    internal static class ObjectSerializer
    {
     
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,   //ReferenceHandler
                                       //   ReferenceHandling = ReferenceHandling.Preserve
             Converters=
             {
                 new DictionaryTKeyEnumTValueConverter()
             }
        };

        public static string ToString(object o)
        {
            return JsonSerializer.Serialize(o, Options);
        }

        public static T FromString<T>(string value)
        {
            return JsonSerializer.Deserialize<T>(value, Options);
        }
    }
    
}
