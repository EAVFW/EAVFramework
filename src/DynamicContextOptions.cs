using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramwork
{
    public class DynamicContextOptions{
        public JToken[] Manifests { get; set; }
        public string PublisherPrefix { get; set; }
        public bool EnableDynamicMigrations { get; set; }

        public string Namespace { get; set; } = $"DynamicModule";
        

    }
}
