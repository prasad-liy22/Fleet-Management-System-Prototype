using FleetManagement.Application.Abstractions;

namespace FleetManagement.Infrastructure.Persistence;

public sealed class SystemAuditActor : IAuditActor
{
    public string ActorId => "system";
}
