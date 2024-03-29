﻿using EAVFramework.Extensions;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAVFramework.Endpoints.Results
{
    public class DataValidationErrorResult : IEndpointResult
    {
        private object data;

        public DataValidationErrorResult(object data)
        {
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteJsonAsync(data);
        }
    }
}
