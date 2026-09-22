using FleetManagement.Domain.Common;

namespace FleetManagement.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    public string Recipient { get; set; } = string.Empty;
    public Guid? RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? RelatedRecordId { get; set; }
    public string? RelatedRecordType { get; set; }
    public bool IsSent { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
}
