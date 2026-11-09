using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Catalog;

public sealed class BrandAdminService : IBrandAdminService
{
    private readonly ApplicationDbContext _db;

    public BrandAdminService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminBrandListItem>> ListAsync(CancellationToken ct = default) =>
        await _db.Brands.AsNoTracking()
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            .Select(b => new AdminBrandListItem
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                LogoUrl = b.LogoUrl,
                DisplayOrder = b.DisplayOrder,
                IsActive = b.IsActive,
                ProductCount = b.Products.Count(p => !p.IsDeleted)
            })
            .ToListAsync(ct);

    public async Task<BrandEditModel?> GetAsync(int id, CancellationToken ct = default) =>
        await _db.Brands.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BrandEditModel
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                Description = b.Description,
                LogoUrl = b.LogoUrl,
                DisplayOrder = b.DisplayOrder,
                IsActive = b.IsActive,
                MetaTitle = b.MetaTitle,
                MetaDescription = b.MetaDescription
            })
            .FirstOrDefaultAsync(ct);

    public async Task<Result<int>> CreateAsync(BrandEditModel model, string? userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return Result<int>.Failure("Name is required.");

        var slug = await SlugHelper.UniqueAsync(model.Slug, model.Name,
            s => _db.Brands.AnyAsync(b => b.Slug == s, ct));

        var entity = new Brand
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Description = model.Description?.Trim(),
            LogoUrl = model.LogoUrl?.Trim(),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            MetaTitle = model.MetaTitle?.Trim(),
            MetaDescription = model.MetaDescription?.Trim()
        };
        _db.Brands.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(BrandEditModel model, string? userId, CancellationToken ct = default)
    {
        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == model.Id, ct);
        if (entity is null) return Result.Failure("Brand not found.");
        if (string.IsNullOrWhiteSpace(model.Name)) return Result.Failure("Name is required.");

        entity.Name = model.Name.Trim();
        entity.Description = model.Description?.Trim();
        entity.LogoUrl = model.LogoUrl?.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsActive = model.IsActive;
        entity.MetaTitle = model.MetaTitle?.Trim();
        entity.MetaDescription = model.MetaDescription?.Trim();

        if (!string.IsNullOrWhiteSpace(model.Slug) &&
            !string.Equals(model.Slug, entity.Slug, StringComparison.OrdinalIgnoreCase))
        {
            entity.Slug = await SlugHelper.UniqueAsync(model.Slug, model.Name,
                s => _db.Brands.AnyAsync(b => b.Slug == s && b.Id != entity.Id, ct));
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default)
    {
        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null) return Result.Failure("Brand not found.");

        var hasProducts = await _db.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.BrandId == id && !p.IsDeleted, ct);
        if (hasProducts)
            return Result.Failure("This brand is still used by products. Reassign them first.");

        _db.Brands.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
