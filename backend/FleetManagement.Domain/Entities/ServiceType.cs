using FleetManagement.Domain.Common;

namespace FleetManagement.Domain.Entities;

public sealed class ServiceType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    public ICollection<ServiceSchedule> ServiceSchedules { get; set; } = new List<ServiceSchedule>();
}
