using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class WishlistItem : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
