namespace FleetManagement.Domain.Common;

public interface IAuditable
{
    string CreatedBy { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    string UpdatedBy { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}

public abstract class AuditableEntity : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
}
