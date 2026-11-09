namespace ECommerce.Application.Catalog;

public sealed class ProductListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? CategoryName { get; init; }
    public string? BrandName { get; init; }
    public decimal Price { get; init; }
    public decimal? DiscountPrice { get; init; }
    public decimal EffectivePrice => DiscountPrice is > 0 && DiscountPrice < Price ? DiscountPrice.Value : Price;
    public int DiscountPercent => Price > 0 && DiscountPrice is > 0 && DiscountPrice < Price
        ? (int)Math.Round((Price - DiscountPrice.Value) / Price * 100)
        : 0;
    public int StockQuantity { get; init; }
    public bool InStock => StockQuantity > 0;
    public bool IsFeatured { get; init; }
    public bool IsNewArrival { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public string? MainImageUrl { get; init; }
}

public sealed class ProductImageDto
{
    public int Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public bool IsMain { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class ProductReviewDto
{
    public int Id { get; init; }
    public string ReviewerName { get; init; } = "Anonymous";
    public byte Rating { get; init; }
    public string? Title { get; init; }
    public string Comment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class ProductDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }

    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public IReadOnlyList<CategoryCrumbDto> CategoryTrail { get; init; } = Array.Empty<CategoryCrumbDto>();

    public string? BrandName { get; init; }
    public string? BrandSlug { get; init; }

    public decimal Price { get; init; }
    public decimal? DiscountPrice { get; init; }
    public decimal EffectivePrice => DiscountPrice is > 0 && DiscountPrice < Price ? DiscountPrice.Value : Price;
    public int DiscountPercent => Price > 0 && DiscountPrice is > 0 && DiscountPrice < Price
        ? (int)Math.Round((Price - DiscountPrice.Value) / Price * 100)
        : 0;

    public int StockQuantity { get; init; }
    public bool InStock => StockQuantity > 0;

    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }

    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();
    public IReadOnlyList<ProductReviewDto> Reviews { get; init; } = Array.Empty<ProductReviewDto>();

    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public sealed class CategoryCrumbDto
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public sealed class CategoryHeaderDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public IReadOnlyList<CategoryCrumbDto> Trail { get; init; } = Array.Empty<CategoryCrumbDto>();
    public IReadOnlyList<CategoryCrumbDto> Children { get; init; } = Array.Empty<CategoryCrumbDto>();
}

public sealed class BrandFacetDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int Count { get; init; }
}
