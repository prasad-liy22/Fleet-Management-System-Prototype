using FleetManagement.Domain.Common;

namespace FleetManagement.Domain.Entities;

public sealed class ServiceSchedule : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid ServiceTypeId { get; set; }
    public ServiceType ServiceType { get; set; } = null!;
    public DateOnly? DueDate { get; set; }
    public long? DueOdometer { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletionLogId { get; set; }
    public MaintenanceLog? CompletionLog { get; set; }
}
