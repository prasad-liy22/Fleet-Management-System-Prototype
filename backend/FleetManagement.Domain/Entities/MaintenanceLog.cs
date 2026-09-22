using FleetManagement.Domain.Common;

namespace FleetManagement.Domain.Entities;

public sealed class MaintenanceLog : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid ServiceTypeId { get; set; }
    public ServiceType ServiceType { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? Parts { get; set; }
    public DateOnly ServiceDate { get; set; }
    public long Odometer { get; set; }
    public string? ReceiptBlobName { get; set; }
}
