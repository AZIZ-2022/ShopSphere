using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

/// <summary>
/// A saved address in a customer's address book. Orders keep their own snapshot,
/// so editing or deleting an address never rewrites order history.
/// </summary>
public class Address : AuditableSoftDeleteEntity
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public AddressType AddressType { get; set; } = AddressType.Both;
    public bool IsDefault { get; set; }
}
