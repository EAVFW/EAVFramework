using Newtonsoft.Json;
using System;

namespace EAVFramework.Endpoints
{
    public class DataUrlConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }



        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var a = new DataUrlHelper();
            a.Parse(reader.ReadAsString());

            return a.Data;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
