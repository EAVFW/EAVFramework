using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetDevOps.Extensions.EAVFramwork
{
    public interface IEntityTypeConfiguration
    {
        void Configure(EntityTypeBuilder builder);
    }
}
