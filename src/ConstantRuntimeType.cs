using EAVFramework.Endpoints.Query.OData;

namespace EAVFramework
{
    public class ConstantRuntimeType : IODataRuntimeType
    {
        private string logicalName;
        private readonly string collectionSchemaName;

        public ConstantRuntimeType(string logicalName, string collectionSchemaName)
        {
            this.logicalName=logicalName;
            this.collectionSchemaName = collectionSchemaName;
        }

        public string GetDataType(object data)
        {
            return logicalName;
        }

        public string GetCollectionSchemaName(object data)
        {
            return collectionSchemaName;
        }
    }
}
