using DotNetDevOps.Extensions.EAVFramwork.Configuration;
using DotNetDevOps.Extensions.EAVFramwork.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramwork.Extensions
{
    public static class HttpResponseExtensions
    {
        public static async Task WriteJsonAsync(this HttpResponse response, object o, string contentType = null)
        {
            var json = ObjectSerializer.ToString(o);
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
