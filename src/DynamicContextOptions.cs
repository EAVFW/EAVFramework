using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace EAVFramework
{
    public class DynamicContextOptions{
        public JToken[] Manifests { get; set; }
        public string PublisherPrefix { get; set; }
        public bool EnableDynamicMigrations { get; set; }
      //  public bool EnableDynamicMigrations { get; set; }

        public string Namespace { get; set; } = $"DynamicModule";
        
        public Assembly DTOAssembly { get; set; }
        public Type[] DTOBaseClasses { get; set; }
        public bool CreateLatestMigration { get; set; } = true;
        public bool WithODATAEntitySet { get; set; } = true;
        public bool UseOnlyExpliciteExternalDTOClases { get; set; }
        public Type[] DisabledPlugins { get; set; }
        public Type[] DTOBaseInterfaces { get;  set; }
    }
}
