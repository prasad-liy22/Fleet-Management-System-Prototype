using System.Security.Claims;
using FleetManagement.Application.Access;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetManagement.Infrastructure.Identity;

public sealed class FleetJwtEvents(IServiceProvider services) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var db = services.GetRequiredService<FleetManagementDbContext>();
        var principal = context.Principal!;
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var id) ||
            !int.TryParse(principal.FindFirstValue("ver"), out var version))
        {
            context.Fail("Invalid session.");
            return;
        }
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
        var role = principal.FindFirstValue("role");
        if (user is null || !user.IsActive || user.TokenVersion != version || !FleetRoles.All.Contains(role ?? ""))
        {
            context.Fail("Invalid session.");
            return;
        }
        var roles = await (from membership in db.UserRoles
                           join storedRole in db.Roles on membership.RoleId equals storedRole.Id
                           where membership.UserId == id
                           select storedRole.Name).ToListAsync(context.HttpContext.RequestAborted);
        if (roles.Count != 1 || roles[0] != role)
        {
            context.Fail("Invalid session.");
            return;
        }
        if (role == FleetRoles.Driver)
        {
            var driverId = await db.Drivers.Where(x => x.ApplicationUserId == id)
                .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(context.HttpContext.RequestAborted);
            if (driverId is null) context.Fail("Invalid session.");
            else ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("driver_id", driverId.Value.ToString()));
        }
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.StatusCode = 401;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 401, Title = "Authentication is required or the session has expired." });
    }

    public override async Task Forbidden(ForbiddenContext context)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 403, Title = "You do not have permission to perform this action." });
    }
}
