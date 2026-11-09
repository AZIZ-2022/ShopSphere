using ECommerce.Application.Catalog;
using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Catalog;

public sealed class ProductAdminService : IProductAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _files;

    public ProductAdminService(ApplicationDbContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    public async Task<PagedResult<AdminProductListItem>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%") || EF.Functions.Like(p.Sku, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminProductListItem
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                CategoryName = p.Category.Name,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                IsNewArrival = p.IsNewArrival,
                MainImageUrl = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync(ct);

        return new PagedResult<AdminProductListItem>(items, page, pageSize, total);
    }

    public async Task<ProductEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;

        return new ProductEditModel
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            CategoryId = p.CategoryId,
            BrandId = p.BrandId,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            StockQuantity = p.StockQuantity,
            IsActive = p.IsActive,
            IsFeatured = p.IsFeatured,
            IsNewArrival = p.IsNewArrival,
            MetaTitle = p.MetaTitle,
            MetaDescription = p.MetaDescription,
            Images = p.Images
                .OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id, ImageUrl = i.ImageUrl, AltText = i.AltText,
                    IsMain = i.IsMain, DisplayOrder = i.DisplayOrder
                })
                .ToList()
        };
    }

    public async Task<IReadOnlyList<(int Id, string Label)>> GetCategoryOptionsAsync(CancellationToken ct = default)
    {
        var rows = await _db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.ParentCategoryId })
            .ToListAsync(ct);
        var byId = rows.ToDictionary(r => r.Id);

        string Label(int id)
        {
            var parts = new List<string>();
            var cur = byId[id];
            var guard = 0;
            while (guard++ < 6)
            {
                parts.Insert(0, cur.Name);
                if (cur.ParentCategoryId is { } pid && byId.TryGetValue(pid, out var parent)) cur = parent;
                else break;
            }
            return string.Join(" › ", parts);
        }

        return rows.OrderBy(r => Label(r.Id)).Select(r => (r.Id, Label(r.Id))).ToList();
    }

    public async Task<IReadOnlyList<(int Id, string Label)>> GetBrandOptionsAsync(CancellationToken ct = default) =>
        (await _db.Brands.AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name }).ToListAsync(ct))
            .Select(b => (b.Id, b.Name))
            .ToList();

    public async Task<Result<int>> CreateAsync(ProductEditModel model, IReadOnlyList<NewProductImage> images, string? userId, CancellationToken ct = default)
    {
        var error = await ValidateAsync(model, ct);
        if (error is not null) return Result<int>.Failure(error);

        var slug = await SlugHelper.UniqueAsync(model.Slug, model.Name,
            s => _db.Products.AnyAsync(p => p.Slug == s, ct));

        if (await _db.Products.AnyAsync(p => p.Sku == model.Sku.Trim(), ct))
            return Result<int>.Failure($"SKU '{model.Sku}' is already in use.");

        var entity = new Product
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Sku = model.Sku.Trim(),
            ShortDescription = model.ShortDescription?.Trim(),
            Description = model.Description,
            CategoryId = model.CategoryId,
            BrandId = model.BrandId,
            Price = model.Price,
            DiscountPrice = NormalizeDiscount(model.Price, model.DiscountPrice),
            StockQuantity = Math.Max(0, model.StockQuantity),
            IsActive = model.IsActive,
            IsFeatured = model.IsFeatured,
            IsNewArrival = model.IsNewArrival,
            MetaTitle = model.MetaTitle?.Trim(),
            MetaDescription = model.MetaDescription?.Trim()
        };

        var order = 0;
        foreach (var img in images)
        {
            entity.Images.Add(new ProductImage
            {
                ImageUrl = img.ImageUrl,
                AltText = string.IsNullOrWhiteSpace(img.AltText) ? entity.Name : img.AltText,
                IsMain = order == 0,
                DisplayOrder = order++
            });
        }

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(ProductEditModel model, IReadOnlyList<NewProductImage> newImages, string? userId, CancellationToken ct = default)
    {
        var entity = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == model.Id, ct);
        if (entity is null) return Result.Failure("Product not found.");

        var error = await ValidateAsync(model, ct);
        if (error is not null) return Result.Failure(error);

        if (!string.Equals(entity.Sku, model.Sku.Trim(), StringComparison.OrdinalIgnoreCase) &&
            await _db.Products.AnyAsync(p => p.Sku == model.Sku.Trim() && p.Id != entity.Id, ct))
            return Result.Failure($"SKU '{model.Sku}' is already in use.");

        entity.Name = model.Name.Trim();
        entity.Sku = model.Sku.Trim();
        entity.ShortDescription = model.ShortDescription?.Trim();
        entity.Description = model.Description;
        entity.CategoryId = model.CategoryId;
        entity.BrandId = model.BrandId;
        entity.Price = model.Price;
        entity.DiscountPrice = NormalizeDiscount(model.Price, model.DiscountPrice);
        entity.StockQuantity = Math.Max(0, model.StockQuantity);
        entity.IsActive = model.IsActive;
        entity.IsFeatured = model.IsFeatured;
        entity.IsNewArrival = model.IsNewArrival;
        entity.MetaTitle = model.MetaTitle?.Trim();
        entity.MetaDescription = model.MetaDescription?.Trim();

        if (!string.IsNullOrWhiteSpace(model.Slug) &&
            !string.Equals(model.Slug, entity.Slug, StringComparison.OrdinalIgnoreCase))
        {
            entity.Slug = await SlugHelper.UniqueAsync(model.Slug, model.Name,
                s => _db.Products.AnyAsync(p => p.Slug == s && p.Id != entity.Id, ct));
        }

        var nextOrder = entity.Images.Count == 0 ? 0 : entity.Images.Max(i => i.DisplayOrder) + 1;
        foreach (var img in newImages)
        {
            entity.Images.Add(new ProductImage
            {
                ImageUrl = img.ImageUrl,
                AltText = string.IsNullOrWhiteSpace(img.AltText) ? entity.Name : img.AltText,
                IsMain = entity.Images.Count == 0,
                DisplayOrder = nextOrder++
            });
        }

        EnsureOneMain(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return Result.Failure("Product not found.");

        _db.Products.Remove(entity); // soft delete
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AddImagesAsync(int productId, IReadOnlyList<NewProductImage> images, CancellationToken ct = default)
    {
        var entity = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (entity is null) return Result.Failure("Product not found.");

        var nextOrder = entity.Images.Count == 0 ? 0 : entity.Images.Max(i => i.DisplayOrder) + 1;
        foreach (var img in images)
        {
            entity.Images.Add(new ProductImage
            {
                ImageUrl = img.ImageUrl,
                AltText = string.IsNullOrWhiteSpace(img.AltText) ? entity.Name : img.AltText,
                IsMain = entity.Images.Count == 0,
                DisplayOrder = nextOrder++
            });
        }
        EnsureOneMain(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var entity = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (entity is null) return Result.Failure("Product not found.");

        var image = entity.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null) return Result.Failure("Image not found.");

        entity.Images.Remove(image);
        _db.Remove(image);
        EnsureOneMain(entity);
        await _db.SaveChangesAsync(ct);

        try { await _files.DeleteAsync(image.ImageUrl, ct); } catch { /* best effort */ }
        return Result.Success();
    }

    public async Task<Result> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var entity = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (entity is null) return Result.Failure("Product not found.");
        if (entity.Images.All(i => i.Id != imageId)) return Result.Failure("Image not found.");

        foreach (var i in entity.Images)
            i.IsMain = i.Id == imageId;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // -------------------------------------------------------------- helpers

    private static decimal? NormalizeDiscount(decimal price, decimal? discount) =>
        discount is > 0 && discount < price ? discount : null;

    private static void EnsureOneMain(Product product)
    {
        if (product.Images.Count == 0) return;
        if (product.Images.Count(i => i.IsMain) == 1) return;

        var first = product.Images.OrderBy(i => i.DisplayOrder).First();
        foreach (var i in product.Images)
            i.IsMain = i == first;
    }

    private async Task<string?> ValidateAsync(ProductEditModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) return "Name is required.";
        if (string.IsNullOrWhiteSpace(model.Sku)) return "SKU is required.";
        if (model.Price <= 0) return "Price must be greater than zero.";
        if (model.DiscountPrice is { } d && d >= model.Price) return "Discount price must be below the regular price.";
        if (!await _db.Categories.AnyAsync(c => c.Id == model.CategoryId, ct)) return "Please choose a valid category.";
        if (model.BrandId is { } bid && !await _db.Brands.AnyAsync(b => b.Id == bid, ct)) return "The selected brand no longer exists.";
        return null;
    }
}
