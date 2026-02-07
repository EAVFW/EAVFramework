using System;

namespace EFCoreTPCMigration.Models
{
    // [EntityInterface(EntityKey = "*")]
    public interface IAuditFields
    {
        public Guid? ModifiedById { get; set; }
        public Guid? CreatedById { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public byte[] RowVersion { get; set; }
    }
}