using DotNetDevOps.Extensions.EAVFramwork.Extensions;
using DotNetDevOps.Extensions.EAVFramwork.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramwork.Endpoints.Results
{
    public class DataEndpointResult : IEndpointResult
    {
        private object data;

        public DataEndpointResult(object data)
        {
            this.data = data;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            await context.Response.WriteJsonAsync(data);
        }
    }
}
