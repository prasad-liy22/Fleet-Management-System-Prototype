using System.Text.Json;
using FleetManagement.Application.Access;

namespace FleetManagement.Infrastructure.Identity;

// A private pickup folder is explicitly configured in Development only. Never served by the API.
public sealed class DevelopmentPasswordResetDelivery(string directory) : IPasswordResetDelivery
{
    public bool IsAvailable => true;

    public async Task DeliverAsync(string recipient, string resetLink, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path,
            JsonSerializer.Serialize(new { recipient, resetLink, createdAt = DateTimeOffset.UtcNow }),
            cancellationToken);
    }
}

public sealed class UnavailablePasswordResetDelivery : IPasswordResetDelivery
{
    public bool IsAvailable => false;
    public Task DeliverAsync(string recipient, string resetLink, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Password reset delivery is not configured.");
}
