using ECommerce.Application.Catalog;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using ECommerce.Web.Models.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public sealed class CatalogController : Controller
{
    private readonly ICatalogQueryService _catalog;

    public CatalogController(ICatalogQueryService catalog) => _catalog = catalog;

    [HttpGet("/c/{slug}")]
    public async Task<IActionResult> Category(string slug, [FromQuery] CatalogFilterInput filter, CancellationToken ct)
    {
        var query = filter.ToQuery();
        query.CategorySlug = slug;

        var result = await _catalog.ListProductsAsync(query, ct);
        if (result.Category is null)
            return NotFound();

        var cat = result.Category;
        var crumbs = cat.Trail
            .Take(Math.Max(0, cat.Trail.Count - 1))
            .Select(c => new BreadcrumbItem(c.Name, $"/c/{c.Slug}"))
            .Append(new BreadcrumbItem(cat.Name))
            .ToArray();
        this.SetBreadcrumb(crumbs);

        return View("List", new CatalogListViewModel
        {
            Kind = CatalogListKind.Category,
            Heading = cat.Name,
            SubHeading = cat.Description,
            BasePath = $"/c/{cat.Slug}",
            Result = result,
            Query = query
        });
    }

    [HttpGet("/search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] CatalogFilterInput filter, CancellationToken ct)
    {
        var query = filter.ToQuery();
        query.SearchTerm = q;

        var result = await _catalog.ListProductsAsync(query, ct);

        this.SetBreadcrumb(new BreadcrumbItem(string.IsNullOrWhiteSpace(q) ? "Search" : $"Search: “{q}”"));

        return View("List", new CatalogListViewModel
        {
            Kind = CatalogListKind.Search,
            Heading = string.IsNullOrWhiteSpace(q) ? "Search" : $"Results for “{q}”",
            SubHeading = $"{result.Products.TotalCount} product(s) found",
            BasePath = "/search",
            Result = result,
            Query = query
        });
    }

    [HttpGet("/deals")]
    public async Task<IActionResult> Deals([FromQuery] CatalogFilterInput filter, CancellationToken ct)
    {
        var query = filter.ToQuery();
        query.DealsOnly = true;
        if (query.Sort == ProductSort.Relevance)
            query.Sort = ProductSort.PriceLowToHigh;

        var result = await _catalog.ListProductsAsync(query, ct);

        this.SetBreadcrumb(new BreadcrumbItem("Deals"));

        return View("List", new CatalogListViewModel
        {
            Kind = CatalogListKind.Deals,
            Heading = "Deals & special offers",
            SubHeading = "Every product below is currently discounted.",
            BasePath = "/deals",
            Result = result,
            Query = query
        });
    }
}

/// <summary>Query-string binding for catalog filters.</summary>
public sealed class CatalogFilterInput
{
    public List<string>? Brand { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public int? Rating { get; set; }
    public bool InStock { get; set; }
    public int Sort { get; set; }
    public int Page { get; set; } = 1;

    public ProductQuery ToQuery() => new()
    {
        Brands = Brand?.Where(b => !string.IsNullOrWhiteSpace(b)).ToArray() ?? Array.Empty<string>(),
        MinPrice = Min is > 0 ? Min : null,
        MaxPrice = Max is > 0 ? Max : null,
        MinRating = Rating is >= 1 and <= 5 ? Rating : null,
        InStockOnly = InStock,
        Sort = Enum.IsDefined(typeof(ProductSort), Sort) ? (ProductSort)Sort : ProductSort.Relevance,
        Page = Page < 1 ? 1 : Page,
        PageSize = 12
    };
}
