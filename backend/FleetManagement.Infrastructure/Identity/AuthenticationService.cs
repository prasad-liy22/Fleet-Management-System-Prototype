using System.Text;
using FleetManagement.Application.Access;
using FleetManagement.Application.Common;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FleetManagement.Infrastructure.Identity;

public sealed class AuthenticationService(
    FleetManagementDbContext db, UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, TokenIssuer tokens,
    IPasswordResetDelivery resetDelivery, IConfiguration configuration, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private const string LoginFailure = "Unable to sign in with these credentials.";

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
            throw new RequestException(401, LoginFailure);
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new RequestException(401, LoginFailure);
        return tokens.Issue(user, await GetCurrentAsync(user.Id));
    }

    public async Task<CurrentUserDto> GetCurrentAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) throw new RequestException(401, "Session is no longer valid.");
        var roles = await users.GetRolesAsync(user);
        if (roles.Count != 1 || !FleetRoles.All.Contains(roles[0]))
            throw new RequestException(401, LoginFailure);
        var driverId = await db.Drivers.Where(x => x.ApplicationUserId == userId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync();
        if (roles[0] == FleetRoles.Driver && driverId is null)
            throw new RequestException(401, LoginFailure);
        return new CurrentUserDto(user.Id, user.DisplayName, user.Email!, roles[0],
            roles[0] == FleetRoles.Driver ? driverId : null);
    }

    public async Task LogoutAsync(Guid userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw new RequestException(401, "Session is no longer valid.");
        user.TokenVersion = checked(user.TokenVersion + 1);
        IdentityResults.RequireSuccess(await users.UpdateAsync(user));
        await transaction.CommitAsync();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // Fail uniformly before looking up an address if production delivery is unavailable.
        if (!resetDelivery.IsAvailable)
            throw new RequestException(503, "Password reset delivery is not configured. Contact your administrator.");
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive) return;
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var baseUrl = configuration["PasswordReset:PublicBaseUrl"]!;
        var link = QueryHelpers.AddQueryString(new Uri(new Uri(baseUrl), "/reset-password").ToString(),
            new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = encoded });
        try { await resetDelivery.DeliverAsync(user.Email!, link); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Delivery errors must not turn the response into an account-existence oracle.
            // Never log the recipient, reset URL, token or password.
            logger.LogError("Password reset delivery failed ({ErrorType}).", exception.GetType().Name);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch (FormatException) { throw new RequestException(400, "The reset link is invalid or expired."); }

        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
            throw new RequestException(400, "The reset link is invalid or expired.");
        var result = await users.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
            throw new RequestException(400, "The reset link is invalid, expired, or the password does not meet the password policy.");
        user.TokenVersion = checked(user.TokenVersion + 1);
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        IdentityResults.RequireSuccess(await users.UpdateAsync(user));
        await transaction.CommitAsync();
    }
}
