using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EFCoreTPCMigration.Models
{
    //    [BaseEntity]
    [Serializable]
    //  [GenericTypeArgument(ArgumentName = "TIdentity", ManifestKey = "Identity")]
    public class BaseOwnerEntity<TIdentity> : BaseIdEntity<TIdentity> where TIdentity : DynamicEntity
    {
        [DataMember(Name = "ownerid")]
        [JsonProperty("ownerid")]
        [JsonPropertyName("ownerid")]
        public virtual Guid? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        [DataMember(Name = "owner")]
        [JsonProperty("owner")]
        [JsonPropertyName("owner")]
        public virtual TIdentity Owner { get; set; }
    }
}