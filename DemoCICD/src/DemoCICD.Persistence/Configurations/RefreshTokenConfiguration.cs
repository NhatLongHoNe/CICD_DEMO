using DemoCICD.Domain.Entities.Identity;
using DemoCICD.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoCICD.Persistence.Configurations;

internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(TableNames.RefreshToken);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ReplacedByToken).HasMaxLength(256);
        builder.HasIndex(x => x.Token);
    }
}
