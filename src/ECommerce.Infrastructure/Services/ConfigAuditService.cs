using System.Text.Json;
using ECommerce.Application.Cms;
using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Home;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Records config changes and can roll one back. Deliberately writes rollbacks directly
/// against the DbContext (rather than calling the admin services for each entity type) to
/// avoid a dependency cycle — the admin services call <see cref="RecordAsync"/> after saving,
/// so this service cannot depend on them in turn.
/// </summary>
public sealed class ConfigAuditService : IConfigAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ISiteSettingsService _settingsCache;
    private readonly IFeatureFlagService _featureFlagsCache;
    private readonly IHomePageService _homeCache;
    private readonly ICmsPageService _cmsPagesCache;

    public ConfigAuditService(
        ApplicationDbContext db,
        ISiteSettingsService settingsCache,
        IFeatureFlagService featureFlagsCache,
        IHomePageService homeCache,
        ICmsPageService cmsPagesCache)
    {
        _db = db;
        _settingsCache = settingsCache;
        _featureFlagsCache = featureFlagsCache;
        _homeCache = homeCache;
        _cmsPagesCache = cmsPagesCache;
    }

    public async Task RecordAsync(string entityType, string entityKey, string? oldValueJson, string? newValueJson, string? changedBy, CancellationToken ct = default)
    {
        // A no-op edit (value unchanged) isn't worth a log row.
        if (string.Equals(oldValueJson, newValueJson, StringComparison.Ordinal))
            return;

        _db.ConfigChangeLogs.Add(new ConfigChangeLog
        {
            EntityType = entityType,
            EntityKey = entityKey,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            CreatedBy = changedBy
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ConfigChangeLogItem>> ListAsync(string? entityType, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ConfigChangeLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new ConfigChangeLogItem
            {
                Id = l.Id,
                EntityType = l.EntityType,
                EntityKey = l.EntityKey,
                OldValueJson = l.OldValueJson,
                NewValueJson = l.NewValueJson,
                CreatedAt = l.CreatedAt,
                CreatedBy = l.CreatedBy
            })
            .ToListAsync(ct);

        return new PagedResult<ConfigChangeLogItem>(items, page, pageSize, total);
    }

    public async Task<Result> RollbackAsync(int logId, string? changedBy, CancellationToken ct = default)
    {
        var log = await _db.ConfigChangeLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == logId, ct);
        if (log is null) return Result.Failure("Change log entry not found.");
        if (string.IsNullOrEmpty(log.OldValueJson)) return Result.Failure("Nothing to roll back to for this entry.");

        var result = log.EntityType switch
        {
            ConfigEntityTypes.SiteSetting => await RollbackSiteSettingAsync(log.EntityKey, log.OldValueJson, ct),
            ConfigEntityTypes.FeatureFlag => await RollbackFeatureFlagAsync(log.EntityKey, log.OldValueJson, ct),
            ConfigEntityTypes.Coupon => await RollbackCouponAsync(log.OldValueJson, ct),
            ConfigEntityTypes.CmsPage => await RollbackCmsPageAsync(log.OldValueJson, ct),
            ConfigEntityTypes.HomeSection => await RollbackHomeSectionAsync(log.OldValueJson, ct),
            _ => Result.Failure("Unknown config type.")
        };

        if (result.Succeeded)
            await RecordAsync(log.EntityType, log.EntityKey, log.NewValueJson, log.OldValueJson, changedBy, ct);

        return result;
    }

    private async Task<Result> RollbackSiteSettingAsync(string key, string oldValue, CancellationToken ct)
    {
        var row = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null) return Result.Failure("Setting not found.");

        row.Value = oldValue;
        await _db.SaveChangesAsync(ct);
        _settingsCache.Invalidate();
        _homeCache.Invalidate();
        return Result.Success();
    }

    private async Task<Result> RollbackFeatureFlagAsync(string key, string oldValue, CancellationToken ct)
    {
        var row = await _db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key, ct);
        if (row is null) return Result.Failure("Feature not found.");

        row.IsEnabled = bool.Parse(oldValue);
        await _db.SaveChangesAsync(ct);
        _featureFlagsCache.Invalidate();
        return Result.Success();
    }

    private async Task<Result> RollbackCouponAsync(string json, CancellationToken ct)
    {
        var snapshot = JsonSerializer.Deserialize<CouponEditModel>(json);
        if (snapshot is null) return Result.Failure("Could not read the saved snapshot.");

        var entity = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == snapshot.Id, ct);
        if (entity is null) return Result.Failure("Coupon not found.");

        entity.Code = snapshot.Code;
        entity.Description = snapshot.Description;
        entity.DiscountType = snapshot.DiscountType;
        entity.DiscountValue = snapshot.DiscountValue;
        entity.MinOrderAmount = snapshot.MinOrderAmount;
        entity.MaxDiscountAmount = snapshot.MaxDiscountAmount;
        entity.UsageLimit = snapshot.UsageLimit;
        entity.StartsAt = snapshot.StartsAt;
        entity.EndsAt = snapshot.EndsAt;
        entity.IsActive = snapshot.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result> RollbackCmsPageAsync(string json, CancellationToken ct)
    {
        var snapshot = JsonSerializer.Deserialize<CmsPageEditModel>(json);
        if (snapshot is null) return Result.Failure("Could not read the saved snapshot.");

        var entity = await _db.CmsPages.FirstOrDefaultAsync(p => p.Id == snapshot.Id, ct);
        if (entity is null) return Result.Failure("Page not found.");

        entity.Title = snapshot.Title;
        entity.BodyHtml = snapshot.BodyHtml;
        entity.MetaTitle = snapshot.MetaTitle;
        entity.MetaDescription = snapshot.MetaDescription;
        entity.IsPublished = snapshot.IsPublished;

        await _db.SaveChangesAsync(ct);
        _cmsPagesCache.Invalidate();
        return Result.Success();
    }

    private async Task<Result> RollbackHomeSectionAsync(string json, CancellationToken ct)
    {
        var snapshot = JsonSerializer.Deserialize<HomeSectionEditModel>(json);
        if (snapshot is null) return Result.Failure("Could not read the saved snapshot.");

        var entity = await _db.HomeSections.FirstOrDefaultAsync(s => s.Id == snapshot.Id, ct);
        if (entity is null) return Result.Failure("Section not found.");

        entity.Title = snapshot.Title;
        entity.Subtitle = snapshot.Subtitle;
        entity.ButtonText = snapshot.ButtonText;
        entity.LinkUrl = snapshot.LinkUrl;
        entity.ConfigJson = snapshot.FeatureItems.Count > 0 ? JsonSerializer.Serialize(snapshot.FeatureItems) : null;

        await _db.SaveChangesAsync(ct);
        _homeCache.Invalidate();
        return Result.Success();
    }
}
