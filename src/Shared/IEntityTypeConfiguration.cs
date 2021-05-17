using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetDevOps.Extensions.EAVFramework
{
    public interface IEntityTypeConfiguration
    {
        void Configure(EntityTypeBuilder builder);
    }


}
