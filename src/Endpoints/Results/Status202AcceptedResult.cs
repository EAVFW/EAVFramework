using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class Status202AcceptedResult : IEndpointResult
    {
        

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
          
        }
    }

    public class StreamResult : IEndpointResult
    {
        private readonly Stream _stream;
        private readonly string contentType;

        public StreamResult(Stream stream, string contentType=null)
        {
            _stream = stream;
            this.contentType = contentType;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
          
            context.Response.ContentType = contentType ?? "application/json; charset=UTF-8";
            
          
            await _stream.CopyToAsync(context.Response.Body);
            await context.Response.Body.FlushAsync();

        }
    }
}
