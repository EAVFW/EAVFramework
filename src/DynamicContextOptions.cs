using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace EAVFramework
{
    public class DynamicContextOptions : MigrationManagerOptions{
        public JToken[] Manifests { get; set; }

        public bool EnableDynamicMigrations { get; set; }

        public bool CreateLatestMigration { get; set; } = true;
        public bool WithODATAEntitySet { get; set; } = true;

        public Type[] DisabledPlugins { get; set; }

    }
}
