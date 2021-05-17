using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace DotNetDevOps.Extensions.EAVFramework
{
    public class DynamicContextOptions{
        public JToken[] Manifests { get; set; }
        public string PublisherPrefix { get; set; }
        public bool EnableDynamicMigrations { get; set; }

        public string Namespace { get; set; } = $"DynamicModule";
        
        public Assembly DTOAssembly { get; set; }
        public Type[] DTOBaseClasses { get; set; }
    }
}
