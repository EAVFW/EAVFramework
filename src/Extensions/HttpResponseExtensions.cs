﻿using EAVFramework.Configuration;
using EAVFramework.Infrastructure;
using Microsoft.AspNetCore.Http;
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
