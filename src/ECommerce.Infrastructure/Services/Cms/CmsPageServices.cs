using System.Text.Json;
using ECommerce.Application.Cms;
using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services.Cms;

public sealed class CmsPageAdminService : ICmsPageAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly ICmsPageService _public;
    private readonly IConfigAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public CmsPageAdminService(ApplicationDbContext db, ICmsPageService @public, IConfigAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _public = @public;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CmsPageListItem>> ListAsync(CancellationToken ct = default) =>
        await _db.CmsPages.AsNoTracking()
            .OrderBy(p => p.Slug)
            .Select(p => new CmsPageListItem { Id = p.Id, Slug = p.Slug, Title = p.Title, IsPublished = p.IsPublished })
            .ToListAsync(ct);

    public async Task<CmsPageEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.CmsPages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;

        var label = CmsPageSlugs.All.FirstOrDefault(x => x.Slug == p.Slug).Label;
        return new CmsPageEditModel
        {
            Id = p.Id,
            Slug = p.Slug,
            Label = string.IsNullOrEmpty(label) ? p.Slug : label,
            Title = p.Title,
            BodyHtml = p.BodyHtml,
            MetaTitle = p.MetaTitle,
            MetaDescription = p.MetaDescription,
            IsPublished = p.IsPublished
        };
    }

    public async Task<Result> UpdateAsync(CmsPageEditModel model, CancellationToken ct = default)
    {
        var entity = await _db.CmsPages.FirstOrDefaultAsync(x => x.Id == model.Id, ct);
        if (entity is null) return Result.Failure("Page not found.");
        if (string.IsNullOrWhiteSpace(model.Title)) return Result.Failure("Title is required.");

        var before = JsonSerializer.Serialize(ToSnapshot(entity));

        entity.Title = model.Title.Trim();
        entity.BodyHtml = model.BodyHtml ?? string.Empty;
        entity.MetaTitle = string.IsNullOrWhiteSpace(model.MetaTitle) ? null : model.MetaTitle.Trim();
        entity.MetaDescription = string.IsNullOrWhiteSpace(model.MetaDescription) ? null : model.MetaDescription.Trim();
        entity.IsPublished = model.IsPublished;

        var after = JsonSerializer.Serialize(ToSnapshot(entity));

        await _db.SaveChangesAsync(ct);
        await _audit.RecordAsync(ConfigEntityTypes.CmsPage, entity.Id.ToString(), before, after, _currentUser.UserName, ct);
        _public.Invalidate();
        return Result.Success();
    }

    private static CmsPageEditModel ToSnapshot(CmsPage p) => new()
    {
        Id = p.Id,
        Slug = p.Slug,
        Title = p.Title,
        BodyHtml = p.BodyHtml,
        MetaTitle = p.MetaTitle,
        MetaDescription = p.MetaDescription,
        IsPublished = p.IsPublished
    };
}

public sealed class CmsPageService : ICmsPageService
{
    private const string CacheKeyPrefix = "cms-page:";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public CmsPageService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public void Invalidate()
    {
        foreach (var (slug, _) in CmsPageSlugs.All)
            _cache.Remove(CacheKeyPrefix + slug);
    }

    public async Task<CmsPageViewDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var key = CacheKeyPrefix + slug;
        if (_cache.TryGetValue(key, out CmsPageViewDto? cached))
            return cached;

        var dto = await _db.CmsPages.AsNoTracking()
            .Where(p => p.Slug == slug && p.IsPublished)
            .Select(p => new CmsPageViewDto
            {
                Title = p.Title,
                BodyHtml = p.BodyHtml,
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription
            })
            .FirstOrDefaultAsync(ct);

        _cache.Set(key, dto, CacheFor);
        return dto;
    }
}
