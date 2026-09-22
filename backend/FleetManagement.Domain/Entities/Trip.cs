using FleetManagement.Domain.Common;
using FleetManagement.Domain.Enums;

namespace FleetManagement.Domain.Entities;

public sealed class Trip : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public long? StartOdometer { get; set; }
    public long? EndOdometer { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? ActualReturnDate { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Draft;
    public string? CancellationReason { get; set; }
    public uint Version { get; private set; }
    public ICollection<DriverNote> DriverNotes { get; set; } = new List<DriverNote>();
}
