using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EFCoreTPCMigration.Models
{
    [Serializable()]
    //  [EntityDTO(LogicalName = "user", Schema = "MC")]
    //  [Entity(LogicalName = "user", SchemaName = "User", CollectionSchemaName = "Users", IsBaseClass = false, EntityKey = "User")]
    public partial class User : Identity, IHasPhone
    {
        public User()
        {
        }

        [DataMember(Name = "age")]
        //     [EntityField(AttributeKey = "Age")]
        [JsonProperty("age")]
        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [DataMember(Name = "allownotifications")]
        //     [EntityField(AttributeKey = "Allow Notifications")]
        [JsonProperty("allownotifications")]
        [JsonPropertyName("allownotifications")]
        public bool? AllowNotifications { get; set; }

        [DataMember(Name = "birthday")]
        //     [EntityField(AttributeKey = "Birthday")]
        [JsonProperty("birthday")]
        [JsonPropertyName("birthday")]
        public DateTime? Birthday { get; set; }

        [DataMember(Name = "email")]
        //     [EntityField(AttributeKey = "Email")]
        [JsonProperty("email")]
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [DataMember(Name = "externalid")]
        //     [EntityField(AttributeKey = "External Id")]
        [JsonProperty("externalid")]
        [JsonPropertyName("externalid")]
        public string ExternalId { get; set; }

        [DataMember(Name = "fieldmetadata")]
        //      [EntityField(AttributeKey = "Field Metadata")]
        [JsonProperty("fieldmetadata")]
        [JsonPropertyName("fieldmetadata")]
        [Description("Metadata field for the info about which fields are locked")]
        public string FieldMetadata { get; set; }

        [DataMember(Name = "firstlogon")]
        //    [EntityField(AttributeKey = "First Logon")]
        [JsonProperty("firstlogon")]
        [JsonPropertyName("firstlogon")]
        public DateTime? FirstLogon { get; set; }

        [DataMember(Name = "lastlogon")]
        //     [EntityField(AttributeKey = "Last Logon")]
        [JsonProperty("lastlogon")]
        [JsonPropertyName("lastlogon")]
        public DateTime? LastLogon { get; set; }

        [DataMember(Name = "nemloginrid")]
        //   [EntityField(AttributeKey = "NemLogin RID")]
        [JsonProperty("nemloginrid")]
        [JsonPropertyName("nemloginrid")]
        public string NemLoginRID { get; set; }

        [DataMember(Name = "password")]
        //  [EntityField(AttributeKey = "Password")]
        [JsonProperty("password")]
        [JsonPropertyName("password")]
        public string Password { get; set; }

        [DataMember(Name = "phone")]
        // [EntityField(AttributeKey = "Phone")]
        [JsonProperty("phone")]
        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [DataMember(Name = "shouldresetpassword")]
        //  [EntityField(AttributeKey = "Should Reset Password")]
        [JsonProperty("shouldresetpassword")]
        [JsonPropertyName("shouldresetpassword")]
        public bool? ShouldResetPassword { get; set; }

        [DataMember(Name = "status")]
        //  [EntityField(AttributeKey = "Status")]
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public UserStatuses? Status { get; set; }


    }
}