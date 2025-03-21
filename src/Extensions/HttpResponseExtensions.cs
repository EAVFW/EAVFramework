using EAVFramework.Configuration;
using EAVFramework.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace EAVFramework.Extensions
{
    public static class HttpResponseExtensions
    {
        public static JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        public static async Task WriteJsonAsync(this HttpResponse response, object o, string contentType = null, Formatting formatting = Formatting.None)
        {
            //var json = ObjectSerializer.ToString(o);
            var serializerSettings= response.HttpContext.RequestServices.GetService<IOptions<EAVFrameworkOptions>>()?.Value.ODataOptions.JsonSerializerSettings;
            serializerSettings.Formatting = formatting; 

            var json = JsonConvert.SerializeObject(o, serializerSettings);

            await response.WriteJsonAsync(json, contentType);
            await response.Body.FlushAsync();
        }
        public static async Task WriteJsonAsync(this HttpResponse response, string json, string contentType = null)
        {
            response.ContentType = contentType ?? "application/json; charset=UTF-8";
            await response.WriteAsync(json);
            await response.Body.FlushAsync();
        }
    }
}
