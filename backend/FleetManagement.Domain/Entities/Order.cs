using FleetManagement.Domain.Common;
using FleetManagement.Domain.Enums;

namespace FleetManagement.Domain.Entities;

public sealed class Order : AuditableEntity
{
    public Guid CustomerCompanyId { get; set; }
    public CustomerCompany CustomerCompany { get; set; } = null!;
    public string PickupLocation { get; set; } = string.Empty;
    public string DeliveryLocation { get; set; } = string.Empty;
    public DateOnly RequiredDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public string? Notes { get; set; }
    public uint Version { get; private set; }
    public Trip? Trip { get; set; }
}
