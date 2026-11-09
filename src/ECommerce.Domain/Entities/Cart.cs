using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// A shopping cart. Either bound to a logged-in user (<see cref="UserId"/>) or to
/// an anonymous browser (<see cref="AnonymousId"/> cookie). On login the guest
/// cart is merged into the user cart.
/// </summary>
public class Cart : AuditableEntity
{
    public string? UserId { get; set; }
    public string? AnonymousId { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
