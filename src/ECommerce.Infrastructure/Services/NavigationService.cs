using ECommerce.Application.Navigation;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services;

public sealed class NavigationService : INavigationService
{
    private const string CacheKey = "nav-category-menu";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public NavigationService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<NavCategoryDto>> GetMenuAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<NavCategoryDto>? cached) && cached is not null)
            return cached;

        var all = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Slug, c.ImageUrl, c.ParentCategoryId })
            .ToListAsync(ct);

        var byParent = all.ToLookup(c => c.ParentCategoryId);

        List<NavCategoryDto> Build(int? parentId) =>
            byParent[parentId]
                .Select(c => new NavCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ImageUrl = c.ImageUrl,
                    Children = Build(c.Id)
                })
                .ToList();

        var menu = Build(null);
        _cache.Set(CacheKey, (IReadOnlyList<NavCategoryDto>)menu, CacheFor);
        return menu;
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}
