using EAVFramework.Endpoints.Query.OData;

namespace EAVFramework
{
    public class ConstantRuntimeType : IODataRuntimeType
    {
        private string logicalName;

        public ConstantRuntimeType(string logicalName)
        {
            this.logicalName=logicalName;
        }

        public string GetDataType(object data)
        {
            return logicalName;
        }
    }
}
