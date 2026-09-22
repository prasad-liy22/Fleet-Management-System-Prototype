using FleetManagement.Domain.Common;

namespace FleetManagement.Domain.Entities;

public sealed class CustomerCompany : SoftDeletableEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
