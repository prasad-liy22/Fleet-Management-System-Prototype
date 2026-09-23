using FleetManagement.Application.Common;
using Microsoft.AspNetCore.Identity;

namespace FleetManagement.Infrastructure.Identity;

internal static class IdentityResults
{
    public static void RequireSuccess(IdentityResult result)
    {
        if (result.Succeeded) return;
        if (result.Errors.Any(e => e.Code is "ConcurrencyFailure" or "DuplicateEmail" or "DuplicateUserName"))
            throw new RequestException(409, "The account conflicts with existing data or was changed. Refresh and try again.");
        throw new RequestException(400, string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
