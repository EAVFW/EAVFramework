using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;

namespace EAVFramework.Hosting
{
    public class EndpointRouteAttribute : Attribute
    {
        public string Name { get; }
        public string Route { get; }
        public string RouteSettingName { get; }

        public EndpointRouteAttribute(string name, string route)
        {
            this.Name = name;
            this.Route = route;
        }

    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true,Inherited =true)]
    public class EndpointRouteMethodAttribute : Attribute
    {
        public string Method { get; }
        public EndpointRouteMethodAttribute(string method)
        {
            Method = method;
        }
    }
    public class HttpGetAttribute : EndpointRouteMethodAttribute
    {
        public HttpGetAttribute() : base(HttpMethods.Get) { }
    }
 
    public class HttpPostAttribute : EndpointRouteMethodAttribute
    {
        public HttpPostAttribute() : base(HttpMethods.Post) { }
    }
}
