using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework
{
    public class DynamicEntity
    {

    }
    public class DynamicMigration
    {
        public DynamicMigration(JToken model, IDynamicTable[] tables)
        {
        }
    }
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(string name)
        {

        }
    }
}