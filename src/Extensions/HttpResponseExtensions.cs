using DotNetDevOps.Extensions.EAVFramework.Configuration;
using DotNetDevOps.Extensions.EAVFramework.Infrastructure;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Extensions
{
    public static class HttpResponseExtensions
    {
        public static async Task WriteJsonAsync(this HttpResponse response, object o, string contentType = null, Formatting formatting = Formatting.None)
        {
            //var json = ObjectSerializer.ToString(o);

            var json = JsonConvert.SerializeObject(o,
                new JsonSerializerSettings { Formatting = formatting, 
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

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
