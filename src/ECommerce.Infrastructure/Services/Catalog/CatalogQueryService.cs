using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Catalog;

public sealed class CatalogQueryService : ICatalogQueryService
{
    private readonly ApplicationDbContext _db;

    public CatalogQueryService(ApplicationDbContext db) => _db = db;

    public async Task<ProductListResult> ListProductsAsync(ProductQuery query, CancellationToken ct = default)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 60);

        CategoryHeaderDto? categoryHeader = null;
        var products = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            var tree = await LoadCategoryTreeAsync(ct);
            var current = tree.Values.FirstOrDefault(c =>
                string.Equals(c.Slug, query.CategorySlug, StringComparison.OrdinalIgnoreCase));

            if (current is null)
                return EmptyResult(query);

            var ids = DescendantIds(tree, current.Id).Append(current.Id).ToHashSet();
            products = products.Where(p => ids.Contains(p.CategoryId));
            categoryHeader = BuildHeader(tree, current);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            products = products.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                (p.ShortDescription != null && EF.Functions.Like(p.ShortDescription, $"%{term}%")) ||
                EF.Functions.Like(p.Sku, $"%{term}%"));
        }

        if (query.DealsOnly)
            products = products.Where(p => p.DiscountPrice != null && p.DiscountPrice > 0 && p.DiscountPrice < p.Price);

        // Facets are computed before the brand/price filters so the sidebar stays useful.
        var facetSource = products;

        var brandFacets = await facetSource
            .Where(p => p.BrandId != null)
            .GroupBy(p => new { p.Brand!.Id, p.Brand.Name, p.Brand.Slug })
            .Select(g => new BrandFacetDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Slug = g.Key.Slug,
                Count = g.Count()
            })
            .OrderByDescending(f => f.Count).ThenBy(f => f.Name)
            .ToListAsync(ct);

        var priceStats = await facetSource
            .Select(p => p.DiscountPrice != null && p.DiscountPrice > 0 && p.DiscountPrice < p.Price
                ? p.DiscountPrice.Value
                : p.Price)
            .ToListAsync(ct);
        var minAvailable = priceStats.Count > 0 ? Math.Floor(priceStats.Min()) : 0m;
        var maxAvailable = priceStats.Count > 0 ? Math.Ceiling(priceStats.Max()) : 0m;

        if (query.Brands.Count > 0)
        {
            var slugs = query.Brands.Select(b => b.ToLowerInvariant()).ToArray();
            products = products.Where(p => p.Brand != null && slugs.Contains(p.Brand.Slug));
        }

        var effectivePrice = products.Select(p => new
        {
            Product = p,
            Effective = p.DiscountPrice != null && p.DiscountPrice > 0 && p.DiscountPrice < p.Price
                ? p.DiscountPrice.Value
                : p.Price
        });

        if (query.MinPrice is { } min)
            effectivePrice = effectivePrice.Where(x => x.Effective >= min);
        if (query.MaxPrice is { } max)
            effectivePrice = effectivePrice.Where(x => x.Effective <= max);
        if (query.MinRating is { } rating)
            effectivePrice = effectivePrice.Where(x => x.Product.AverageRating >= rating);
        if (query.InStockOnly)
            effectivePrice = effectivePrice.Where(x => x.Product.StockQuantity > 0);

        effectivePrice = query.Sort switch
        {
            ProductSort.PriceLowToHigh => effectivePrice.OrderBy(x => x.Effective),
            ProductSort.PriceHighToLow => effectivePrice.OrderByDescending(x => x.Effective),
            ProductSort.Newest => effectivePrice.OrderByDescending(x => x.Product.CreatedAt),
            ProductSort.TopRated => effectivePrice.OrderByDescending(x => x.Product.AverageRating).ThenByDescending(x => x.Product.ReviewCount),
            ProductSort.BestSelling => effectivePrice.OrderByDescending(x => x.Product.SoldCount),
            _ => effectivePrice.OrderByDescending(x => x.Product.IsFeatured).ThenByDescending(x => x.Product.SoldCount)
        };

        var total = await effectivePrice.CountAsync(ct);

        var items = await effectivePrice
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ProductListItemDto
            {
                Id = x.Product.Id,
                Name = x.Product.Name,
                Slug = x.Product.Slug,
                CategoryName = x.Product.Category.Name,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : null,
                Price = x.Product.Price,
                DiscountPrice = x.Product.DiscountPrice,
                StockQuantity = x.Product.StockQuantity,
                IsFeatured = x.Product.IsFeatured,
                IsNewArrival = x.Product.IsNewArrival,
                AverageRating = x.Product.AverageRating,
                ReviewCount = x.Product.ReviewCount,
                MainImageUrl = x.Product.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync(ct);

        return new ProductListResult
        {
            Products = new PagedResult<ProductListItemDto>(items, query.Page, query.PageSize, total),
            BrandFacets = brandFacets,
            MinPriceAvailable = minAvailable,
            MaxPriceAvailable = maxAvailable,
            Category = categoryHeader
        };
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, ct);

        if (product is null)
            return null;

        var reviews = await _db.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ReviewerName = r.ReviewerName ?? "Anonymous",
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        var tree = await LoadCategoryTreeAsync(ct);
        var trail = tree.TryGetValue(product.CategoryId, out var node)
            ? BuildTrail(tree, node)
            : Array.Empty<CategoryCrumbDto>();

        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategorySlug = product.Category.Slug,
            CategoryTrail = trail,
            BrandName = product.Brand?.Name,
            BrandSlug = product.Brand?.Slug,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            StockQuantity = product.StockQuantity,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            Images = product.Images
                .OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText,
                    IsMain = i.IsMain,
                    DisplayOrder = i.DisplayOrder
                })
                .ToList(),
            Reviews = reviews
        };
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetRelatedAsync(int productId, int take = 4, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId })
            .FirstOrDefaultAsync(ct);

        if (product is null)
            return Array.Empty<ProductListItemDto>();

        return await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != productId)
            .OrderByDescending(p => p.SoldCount).ThenByDescending(p => p.AverageRating)
            .Take(take)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                CategoryName = p.Category.Name,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                StockQuantity = p.StockQuantity,
                IsFeatured = p.IsFeatured,
                IsNewArrival = p.IsNewArrival,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                MainImageUrl = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync(ct);
    }

    public Task IncrementViewCountAsync(int productId, CancellationToken ct = default) =>
        _db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

    public async Task<CategoryHeaderDto?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tree = await LoadCategoryTreeAsync(ct);
        var node = tree.Values.FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return node is null ? null : BuildHeader(tree, node);
    }

    // ------------------------------------------------------------- helpers

    private sealed record CatNode(int Id, string Name, string Slug, string? Description, string? ImageUrl, int? ParentId, bool IsActive, string? MetaTitle, string? MetaDescription);

    private async Task<Dictionary<int, CatNode>> LoadCategoryTreeAsync(CancellationToken ct)
    {
        var rows = await _db.Categories.AsNoTracking()
            .Select(c => new CatNode(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.ParentCategoryId, c.IsActive, c.MetaTitle, c.MetaDescription))
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id);
    }

    private static IEnumerable<int> DescendantIds(Dictionary<int, CatNode> tree, int rootId)
    {
        var childrenByParent = tree.Values.ToLookup(c => c.ParentId);
        var stack = new Stack<int>(childrenByParent[rootId].Select(c => c.Id));
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            yield return id;
            foreach (var child in childrenByParent[id])
                stack.Push(child.Id);
        }
    }

    private static IReadOnlyList<CategoryCrumbDto> BuildTrail(Dictionary<int, CatNode> tree, CatNode node)
    {
        var crumbs = new List<CategoryCrumbDto>();
        var current = node;
        var guard = 0;
        while (current is not null && guard++ < 10)
        {
            crumbs.Insert(0, new CategoryCrumbDto { Name = current.Name, Slug = current.Slug });
            current = current.ParentId is { } pid && tree.TryGetValue(pid, out var parent) ? parent : null;
        }
        return crumbs;
    }

    private static CategoryHeaderDto BuildHeader(Dictionary<int, CatNode> tree, CatNode node)
    {
        var children = tree.Values
            .Where(c => c.ParentId == node.Id && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryCrumbDto { Name = c.Name, Slug = c.Slug })
            .ToList();

        return new CategoryHeaderDto
        {
            Id = node.Id,
            Name = node.Name,
            Slug = node.Slug,
            Description = node.Description,
            ImageUrl = node.ImageUrl,
            MetaTitle = node.MetaTitle,
            MetaDescription = node.MetaDescription,
            Trail = BuildTrail(tree, node),
            Children = children
        };
    }

    private static ProductListResult EmptyResult(ProductQuery query) => new()
    {
        Products = new PagedResult<ProductListItemDto>(Array.Empty<ProductListItemDto>(), query.Page, query.PageSize, 0)
    };
}
