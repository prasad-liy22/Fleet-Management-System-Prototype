using System.Security.Claims;
using FleetManagement.Application.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authentication) : ControllerBase
{
    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("authentication")]
    public Task<LoginResponse> Login(LoginRequest request) => authentication.LoginAsync(request);

    [Authorize, HttpGet("me")]
    public Task<CurrentUserDto> Me() => authentication.GetCurrentAsync(UserId);

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authentication.LogoutAsync(UserId);
        return NoContent();
    }

    [AllowAnonymous, HttpPost("forgot-password"), EnableRateLimiting("authentication")]
    public async Task<MessageResponse> ForgotPassword(ForgotPasswordRequest request)
    {
        await authentication.ForgotPasswordAsync(request);
        return new MessageResponse("If the account is eligible, password reset instructions will be delivered.");
    }

    [AllowAnonymous, HttpPost("reset-password"), EnableRateLimiting("authentication")]
    public async Task<MessageResponse> ResetPassword(ResetPasswordRequest request)
    {
        await authentication.ResetPasswordAsync(request);
        return new MessageResponse("Password updated. Sign in with your new password.");
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue("sub")!);
}
