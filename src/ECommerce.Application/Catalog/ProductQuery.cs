namespace ECommerce.Application.Catalog;

public enum ProductSort
{
    Relevance = 0,
    PriceLowToHigh = 1,
    PriceHighToLow = 2,
    Newest = 3,
    TopRated = 4,
    BestSelling = 5
}

/// <summary>Filter / sort / paging options for a product listing (category, search or deals).</summary>
public sealed class ProductQuery
{
    public string? CategorySlug { get; set; }
    public string? SearchTerm { get; set; }
    public bool DealsOnly { get; set; }

    public IReadOnlyList<string> Brands { get; set; } = Array.Empty<string>();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public bool InStockOnly { get; set; }

    public ProductSort Sort { get; set; } = ProductSort.Relevance;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public sealed class ProductListResult
{
    public required Common.Models.PagedResult<ProductListItemDto> Products { get; init; }
    public IReadOnlyList<BrandFacetDto> BrandFacets { get; init; } = Array.Empty<BrandFacetDto>();
    public decimal MinPriceAvailable { get; init; }
    public decimal MaxPriceAvailable { get; init; }
    public CategoryHeaderDto? Category { get; init; }
}
