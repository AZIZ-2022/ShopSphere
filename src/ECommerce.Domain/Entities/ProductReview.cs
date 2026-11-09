using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ProductReview : AuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>ASP.NET Identity user id (string key).</summary>
    public string UserId { get; set; } = string.Empty;
    public string? ReviewerName { get; set; }

    /// <summary>1 - 5.</summary>
    public byte Rating { get; set; }
    public string? Title { get; set; }
    public string Comment { get; set; } = string.Empty;

    public bool IsApproved { get; set; }
}
