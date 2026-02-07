using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EFCoreTPCMigration.Models
{
    [Serializable()]
    //  [EntityDTO(LogicalName = "securitygroup", Schema = "MC")]
    // [Entity(LogicalName = "securitygroup", SchemaName = "SecurityGroup", CollectionSchemaName = "SecurityGroups", IsBaseClass = false, EntityKey = "Security Group")]
    public partial class SecurityGroup : Identity, ISecurityGroup
    {
        public SecurityGroup()
        {
        }

        [DataMember(Name = "externalid")]
        // [EntityField(AttributeKey = "External Id")]
        [JsonProperty("externalid")]
        [JsonPropertyName("externalid")]
        public string ExternalId { get; set; }

        [DataMember(Name = "isbusinessunit")]
        //  [EntityField(AttributeKey = "Is Business Unit")]
        [JsonProperty("isbusinessunit")]
        [JsonPropertyName("isbusinessunit")]
        public bool? IsBusinessUnit { get; set; }



    }
}