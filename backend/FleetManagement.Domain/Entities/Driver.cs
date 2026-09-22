using FleetManagement.Domain.Common;
using FleetManagement.Domain.Enums;

namespace FleetManagement.Domain.Entities;

public sealed class Driver : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string LicenceNumber { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    // Identity lives in Infrastructure; the domain keeps only its optional identifier.
    public Guid? ApplicationUserId { get; set; }
    public DriverStatus Status { get; set; } = DriverStatus.Available;
    public uint Version { get; private set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<DriverNote> Notes { get; set; } = new List<DriverNote>();
}
