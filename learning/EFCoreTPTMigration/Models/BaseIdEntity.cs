using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EFCoreTPCMigration.Models
{
    //  [BaseEntity]
    [Serializable]
    // [GenericTypeArgument(ArgumentName = "TIdentity", ManifestKey = "Identity")]
    public class BaseIdEntity<TIdentity> : DynamicEntity where TIdentity : DynamicEntity
    {

        [DataMember(Name = "id")]
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [DataMember(Name = "modifiedbyid")]
        [JsonProperty("modifiedbyid")]
        [JsonPropertyName("modifiedbyid")]
        public virtual Guid? ModifiedById { get; set; }

        [ForeignKey("ModifiedById")]
        [JsonProperty("modifiedby")]
        [JsonPropertyName("modifiedby")]
        [DataMember(Name = "modifiedby")]
        public virtual TIdentity ModifiedBy { get; set; }

        [DataMember(Name = "createdbyid")]
        [JsonProperty("createdbyid")]
        [JsonPropertyName("createdbyid")]
        public virtual Guid? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        [JsonProperty("createdby")]
        [JsonPropertyName("createdby")]
        [DataMember(Name = "createdby")]
        public virtual TIdentity CreatedBy { get; set; }

        [DataMember(Name = "modifiedon")]
        [JsonProperty("modifiedon")]
        [JsonPropertyName("modifiedon")]
        public virtual DateTime? ModifiedOn { get; set; }

        [DataMember(Name = "createdon")]
        [JsonProperty("createdon")]
        [JsonPropertyName("createdon")]
        public virtual DateTime? CreatedOn { get; set; }

        [DataMember(Name = "rowversion")]
        [JsonProperty("rowversion")]
        [JsonPropertyName("rowversion")]
        public virtual byte[] RowVersion { get; set; }

    }
}