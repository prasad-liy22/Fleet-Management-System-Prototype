using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders", t => t.HasCheckConstraint("CK_Orders_Status",
            "\"Status\" IN ('Draft', 'Assigned', 'InProgress', 'Completed', 'Cancelled')"));
        b.Property(x => x.PickupLocation).HasMaxLength(300).IsRequired();
        b.Property(x => x.DeliveryLocation).HasMaxLength(300).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Version).IsRowVersion();
        b.HasOne(x => x.CustomerCompany).WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerCompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Status, x.RequiredDate });
    }
}
