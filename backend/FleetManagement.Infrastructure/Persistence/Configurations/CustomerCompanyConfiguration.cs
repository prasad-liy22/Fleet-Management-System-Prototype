using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class CustomerCompanyConfiguration : IEntityTypeConfiguration<CustomerCompany>
{
    public void Configure(EntityTypeBuilder<CustomerCompany> b)
    {
        b.ToTable("CustomerCompanies");
        b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ContactPerson).HasMaxLength(160).IsRequired();
        b.Property(x => x.Telephone).HasMaxLength(40).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500).IsRequired();
        b.HasIndex(x => x.CompanyName);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
