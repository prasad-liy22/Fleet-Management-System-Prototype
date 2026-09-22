namespace FleetManagement.Application.Abstractions;

/// <summary>Supplies the trusted actor identifier for persistence auditing.</summary>
public interface IAuditActor
{
    string ActorId { get; }
}
