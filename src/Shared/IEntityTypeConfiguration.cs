using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EAVFramework
{
    public interface IEntityTypeConfiguration
    {
        void Configure(EntityTypeBuilder builder);
    }


}
