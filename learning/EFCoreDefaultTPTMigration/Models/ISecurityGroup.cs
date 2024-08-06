using System;

namespace EFCoreTPCMigration.Models
{
    // [EntityInterface(EntityKey = "Security Group")]
    public interface ISecurityGroup
    {
        public Guid Id { get; set; }
        public bool? IsBusinessUnit { get; set; }
    }
}