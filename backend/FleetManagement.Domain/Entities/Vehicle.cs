using FleetManagement.Domain.Common;
using FleetManagement.Domain.Enums;

namespace FleetManagement.Domain.Entities;

public sealed class Vehicle : SoftDeletableEntity
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Capacity { get; set; }
    public long CurrentOdometer { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    public uint Version { get; private set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    public ICollection<ServiceSchedule> ServiceSchedules { get; set; } = new List<ServiceSchedule>();
}
