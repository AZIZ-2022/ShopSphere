using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// A sellable catalog item. Prices are stored in the site base currency (USD);
/// display conversion happens in the presentation layer.
/// </summary>
public class Product : AuditableSoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }

    public int StockQuantity { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Denormalised rating cache, recalculated when reviews change.</summary>
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ViewCount { get; set; }
    public int SoldCount { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

    /// <summary>Effective unit price after any active discount.</summary>
    public decimal EffectivePrice =>
        DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price
            ? DiscountPrice.Value
            : Price;
}
