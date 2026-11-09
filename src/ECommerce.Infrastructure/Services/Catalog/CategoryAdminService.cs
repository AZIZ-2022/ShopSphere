using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Navigation;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Catalog;

public sealed class CategoryAdminService : ICategoryAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly INavigationService _navigation;

    public CategoryAdminService(ApplicationDbContext db, INavigationService navigation)
    {
        _db = db;
        _navigation = navigation;
    }

    public async Task<IReadOnlyList<AdminCategoryListItem>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _db.Categories.AsNoTracking()
            .Select(c => new
            {
                c.Id, c.Name, c.Slug, c.ParentCategoryId, c.DisplayOrder, c.IsActive,
                ProductCount = c.Products.Count(p => !p.IsDeleted)
            })
            .ToListAsync(ct);

        var byId = rows.ToDictionary(r => r.Id);
        var childrenByParent = rows.ToLookup(r => r.ParentCategoryId);
        var result = new List<AdminCategoryListItem>();

        void Walk(int? parentId, int depth)
        {
            foreach (var row in childrenByParent[parentId].OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name))
            {
                result.Add(new AdminCategoryListItem
                {
                    Id = row.Id,
                    Name = row.Name,
                    Slug = row.Slug,
                    ParentCategoryId = row.ParentCategoryId,
                    ParentName = row.ParentCategoryId is { } pid && byId.TryGetValue(pid, out var p) ? p.Name : null,
                    Depth = depth,
                    DisplayOrder = row.DisplayOrder,
                    IsActive = row.IsActive,
                    ProductCount = row.ProductCount
                });
                Walk(row.Id, depth + 1);
            }
        }

        Walk(null, 0);
        return result;
    }

    public async Task<CategoryEditModel?> GetAsync(int id, CancellationToken ct = default) =>
        await _db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryEditModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                MetaTitle = c.MetaTitle,
                MetaDescription = c.MetaDescription
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<(int Id, string Label)>> GetParentOptionsAsync(int? excludingId = null, CancellationToken ct = default)
    {
        var rows = await _db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.ParentCategoryId, c.DisplayOrder })
            .ToListAsync(ct);

        var blocked = new HashSet<int>();
        if (excludingId is { } ex)
        {
            blocked.Add(ex);
            var childrenByParent = rows.ToLookup(r => r.ParentCategoryId);
            var stack = new Stack<int>(childrenByParent[ex].Select(r => r.Id));
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                blocked.Add(id);
                foreach (var child in childrenByParent[id]) stack.Push(child.Id);
            }
        }

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

        return rows
            .Where(r => !blocked.Contains(r.Id))
            .OrderBy(r => Label(r.Id))
            .Select(r => (r.Id, Label(r.Id)))
            .ToList();
    }

    public async Task<Result<int>> CreateAsync(CategoryEditModel model, string? userId, CancellationToken ct = default)
    {
        var validation = await ValidateAsync(model, ct);
        if (validation is not null) return Result<int>.Failure(validation);

        var slug = await SlugHelper.UniqueAsync(model.Slug, model.Name,
            s => _db.Categories.AnyAsync(c => c.Slug == s, ct));

        var entity = new Category
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Description = model.Description?.Trim(),
            ImageUrl = model.ImageUrl?.Trim(),
            ParentCategoryId = model.ParentCategoryId,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            MetaTitle = model.MetaTitle?.Trim(),
            MetaDescription = model.MetaDescription?.Trim()
        };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        _navigation.Invalidate();
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(CategoryEditModel model, string? userId, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.Id, ct);
        if (entity is null) return Result.Failure("Category not found.");

        var validation = await ValidateAsync(model, ct);
        if (validation is not null) return Result.Failure(validation);

        if (model.ParentCategoryId == model.Id)
            return Result.Failure("A category cannot be its own parent.");

        entity.Name = model.Name.Trim();
        entity.Description = model.Description?.Trim();
        entity.ImageUrl = model.ImageUrl?.Trim();
        entity.ParentCategoryId = model.ParentCategoryId;
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsActive = model.IsActive;
        entity.MetaTitle = model.MetaTitle?.Trim();
        entity.MetaDescription = model.MetaDescription?.Trim();

        if (!string.IsNullOrWhiteSpace(model.Slug) &&
            !string.Equals(model.Slug, entity.Slug, StringComparison.OrdinalIgnoreCase))
        {
            entity.Slug = await SlugHelper.UniqueAsync(model.Slug, model.Name,
                s => _db.Categories.AnyAsync(c => c.Slug == s && c.Id != entity.Id, ct));
        }

        await _db.SaveChangesAsync(ct);
        _navigation.Invalidate();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return Result.Failure("Category not found.");

        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentCategoryId == id, ct);
        if (hasChildren)
            return Result.Failure("Move or delete the sub-categories first.");

        var hasProducts = await _db.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.CategoryId == id && !p.IsDeleted, ct);
        if (hasProducts)
            return Result.Failure("This category still has products. Reassign them first.");

        _db.Categories.Remove(entity); // soft delete via interceptor
        await _db.SaveChangesAsync(ct);
        _navigation.Invalidate();
        return Result.Success();
    }

    private async Task<string?> ValidateAsync(CategoryEditModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return "Name is required.";
        if (model.ParentCategoryId is { } pid && !await _db.Categories.AnyAsync(c => c.Id == pid, ct))
            return "The selected parent category no longer exists.";
        return null;
    }
}
