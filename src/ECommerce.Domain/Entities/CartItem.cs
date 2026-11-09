using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class CartItem : AuditableEntity
{
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Unit price (base currency) captured when the item was added.</summary>
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
