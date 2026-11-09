using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Product category. Self-referencing to support a multi-level navigation tree
/// (the storefront navbar uses up to 3 levels).
/// </summary>
public class Category : AuditableSoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
