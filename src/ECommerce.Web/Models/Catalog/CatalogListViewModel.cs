using ECommerce.Application.Catalog;

namespace ECommerce.Web.Models.Catalog;

public enum CatalogListKind { Category, Search, Deals }

public sealed class CatalogListViewModel
{
    public required CatalogListKind Kind { get; init; }
    public required string Heading { get; init; }
    public string? SubHeading { get; init; }

    /// <summary>Path without query string, e.g. <c>/c/electronics</c>, <c>/search</c>, <c>/deals</c>.</summary>
    public required string BasePath { get; init; }

    public required ProductListResult Result { get; init; }
    public required ProductQuery Query { get; init; }

    public IReadOnlyList<(ProductSort Value, string Label)> SortOptions { get; } = new[]
    {
        (ProductSort.Relevance, "Relevance"),
        (ProductSort.PriceLowToHigh, "Price: low to high"),
        (ProductSort.PriceHighToLow, "Price: high to low"),
        (ProductSort.Newest, "Newest"),
        (ProductSort.TopRated, "Top rated"),
        (ProductSort.BestSelling, "Best selling"),
    };

    /// <summary>Builds a URL for this listing with some query values overridden.</summary>
    public string BuildUrl(
        int? page = null,
        ProductSort? sort = null,
        string? toggleBrand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool clearPrice = false,
        bool? inStock = null,
        int? minRating = null,
        bool clearRating = false)
    {
        var q = Query;
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(q.SearchTerm))
            parts.Add($"q={Uri.EscapeDataString(q.SearchTerm)}");

        var brands = new HashSet<string>(q.Brands, StringComparer.OrdinalIgnoreCase);
        if (toggleBrand is not null)
        {
            if (!brands.Remove(toggleBrand)) brands.Add(toggleBrand);
        }
        foreach (var b in brands)
            parts.Add($"brand={Uri.EscapeDataString(b)}");

        var min = clearPrice ? null : minPrice ?? q.MinPrice;
        var max = clearPrice ? null : maxPrice ?? q.MaxPrice;
        if (min is { } mn) parts.Add($"min={mn:0.##}");
        if (max is { } mx) parts.Add($"max={mx:0.##}");

        var rating = clearRating ? null : minRating ?? q.MinRating;
        if (rating is { } r) parts.Add($"rating={r}");

        var stock = inStock ?? q.InStockOnly;
        if (stock) parts.Add("instock=true");

        var theSort = sort ?? q.Sort;
        if (theSort != ProductSort.Relevance) parts.Add($"sort={(int)theSort}");

        var thePage = page ?? q.Page;
        if (thePage > 1) parts.Add($"page={thePage}");

        return parts.Count == 0 ? BasePath : $"{BasePath}?{string.Join("&", parts)}";
    }

    public bool BrandSelected(string slug) =>
        Query.Brands.Contains(slug, StringComparer.OrdinalIgnoreCase);

    public bool HasActiveFilters =>
        Query.Brands.Count > 0 || Query.MinPrice is not null || Query.MaxPrice is not null
        || Query.MinRating is not null || Query.InStockOnly;
}
