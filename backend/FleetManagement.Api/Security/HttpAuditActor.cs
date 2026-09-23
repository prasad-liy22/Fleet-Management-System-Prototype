using System.Security.Claims;
using FleetManagement.Application.Abstractions;

namespace FleetManagement.Api.Security;

public sealed class HttpAuditActor(IHttpContextAccessor accessor) : IAuditActor
{
    public string ActorId => accessor.HttpContext?.User is { Identity.IsAuthenticated: true } principal
        ? principal.FindFirstValue("sub") ?? throw new InvalidOperationException("Authenticated identity has no subject.")
        : "system";
}
