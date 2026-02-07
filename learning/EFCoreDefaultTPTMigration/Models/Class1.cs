using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text.Json.Serialization;

namespace EFCoreTPCMigration.Models
{

    [Serializable()]
    // [EntityDTO(LogicalName = "identity", Schema = "MC")]
    // [Entity(LogicalName = "identity", SchemaName = "Identity", CollectionSchemaName = "Identities", IsBaseClass = true, EntityKey = "Identity")]
    public partial class Identity : BaseOwnerEntity<Identity>, IIdentity, IAuditFields
    {
        public Identity()
        {
        }

        [DataMember(Name = "name")]
        // [EntityField(AttributeKey = "Name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        //  [PrimaryField()]
        public string Name { get; set; }


    }
}