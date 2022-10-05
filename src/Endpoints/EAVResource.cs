using Microsoft.AspNetCore.Http;
using System;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class EAVResource
    {
        
        public Type EntityType { get; set; }
        public string EntityCollectionSchemaName { get;  set; }
        public HostString Host { get; internal set; }
    }
}
