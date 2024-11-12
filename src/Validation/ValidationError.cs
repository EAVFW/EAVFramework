using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace EAVFramework.Validation
{
    public class CoreError
    {
        public string Error { get; set; }
        public string Code { get; set; }
        public object[] ErrorArgs { get; set; }
        public string AttributeSchemaName { get; set; }
        public string EntityCollectionSchemaName { get; set; }
    }
    public class ValidationError : CoreError
    {
        
       
    }
    public class AuthorizationError : CoreError
    {

         
    }
}