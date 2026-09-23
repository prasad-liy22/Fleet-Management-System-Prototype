using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Application.Access;

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(128)] string Password);
public sealed record ForgotPasswordRequest([Required, EmailAddress, MaxLength(254)] string Email);
public sealed record ResetPasswordRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(8192)] string Token,
    [Required, MinLength(12), MaxLength(128)] string Password);
public sealed record CurrentUserDto(Guid Id, string DisplayName, string Email, string Role, Guid? DriverId);
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserDto User);
public sealed record MessageResponse(string Message);

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<CurrentUserDto> GetCurrentAsync(Guid userId);
    Task LogoutAsync(Guid userId);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}

public interface IPasswordResetDelivery
{
    bool IsAvailable { get; }
    Task DeliverAsync(string recipient, string resetLink, CancellationToken cancellationToken = default);
}
