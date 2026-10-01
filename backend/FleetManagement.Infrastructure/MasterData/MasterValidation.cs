using System.ComponentModel.DataAnnotations;
using FleetManagement.Application.Common;
using FleetManagement.Application.MasterData;
using FleetManagement.Domain.Common;

namespace FleetManagement.Infrastructure.MasterData;

internal static class MasterValidation
{
    public static string Text(string? value, int max, string name, bool required = true)
    {
        var result = value?.Trim() ?? "";
        if ((required && result.Length == 0) || result.Length > max || result.Any(char.IsControl))
            throw new RequestException(400, $"{name} must {(required ? "be provided and " : "")}contain at most {max} characters without control characters.");
        return result;
    }
    public static string Contact(string? value, string name, bool required = true)
    {
        var result = Text(value, 40, name, required);
        if (result.Length > 0 && (!result.Any(char.IsDigit) || result.Any(c => !char.IsDigit(c) && !" +()-./xX#".Contains(c))))
            throw new RequestException(400, $"{name} must contain a phone number; international prefixes and extensions are supported.");
        return result;
    }
    public static T Status<T>(string? value) where T : struct, Enum
    {
        if (value is null || !Enum.GetNames<T>().Contains(value) || !Enum.TryParse<T>(value, out var status))
            throw new RequestException(400, "Choose a supported status.");
        return status;
    }
    public static void Version(uint current, uint supplied)
    {
        if (supplied == 0 || current != supplied)
            throw new RequestException(409, "This record changed. Reload the latest record before trying again.");
    }
    public static void Editable(SoftDeletableEntity entity)
    {
        if (entity.IsDeleted) throw new RequestException(409, "Reactivate this record before editing it.");
    }
    public static void Email(string value)
    {
        if (value.Length > 0 && !new EmailAddressAttribute().IsValid(value))
            throw new RequestException(400, "Provide a valid email address.");
    }
    public static AuditDto Audit(IAuditable entity) => new(entity.CreatedBy, entity.CreatedAt, entity.UpdatedBy, entity.UpdatedAt);
}
