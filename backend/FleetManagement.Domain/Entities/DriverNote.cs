using FleetManagement.Domain.Common;

namespace FleetManagement.Domain.Entities;

public sealed class DriverNote : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public string Note { get; set; } = string.Empty;
    public bool RequiresAttention { get; set; } = true;
    public bool IsReviewed { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
}
