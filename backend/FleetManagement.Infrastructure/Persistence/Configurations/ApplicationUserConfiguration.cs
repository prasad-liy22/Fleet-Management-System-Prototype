using FleetManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        b.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
    }
}
