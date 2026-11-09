using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Manufacturer / brand a product belongs to. Optional on a product.
/// </summary>
public class Brand : AuditableSoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
