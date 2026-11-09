using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private const string CacheKey = "feature-flags-snapshot";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public FeatureFlagService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    public async Task<bool> IsEnabledAsync(string key, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        // Unknown/not-yet-seeded keys default to enabled — a missing row must never silently
        // disable a feature that hasn't been explicitly turned off by an admin.
        return !all.TryGetValue(key, out var enabled) || enabled;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, bool>? cached) && cached is not null)
            return cached;

        var map = await _db.FeatureFlags.AsNoTracking()
            .ToDictionaryAsync(f => f.Key, f => f.IsEnabled, StringComparer.OrdinalIgnoreCase, ct);

        _cache.Set(CacheKey, map, CacheFor);
        return map;
    }
}

public sealed class FeatureFlagAdminService : IFeatureFlagAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IFeatureFlagService _public;
    private readonly IConfigAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public FeatureFlagAdminService(ApplicationDbContext db, IFeatureFlagService @public, IConfigAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _public = @public;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FeatureFlagListItem>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _db.FeatureFlags.AsNoTracking()
            .ToDictionaryAsync(f => f.Key, StringComparer.OrdinalIgnoreCase, ct);

        return FeatureFlagKeys.All
            .Select(k => new FeatureFlagListItem
            {
                Key = k.Key,
                Label = k.Label,
                Description = k.Description,
                IsEnabled = !rows.TryGetValue(k.Key, out var row) || row.IsEnabled
            })
            .ToList();
    }

    public async Task<Result> SetEnabledAsync(string key, bool enabled, CancellationToken ct = default)
    {
        var entity = await _db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key, ct);
        var wasEnabled = true; // default-enabled when the row doesn't exist yet
        if (entity is null)
        {
            var meta = FeatureFlagKeys.All.FirstOrDefault(k => k.Key == key);
            if (meta == default) return Result.Failure("Unknown feature.");

            entity = new FeatureFlag { Key = key, Description = meta.Description };
            _db.FeatureFlags.Add(entity);
        }
        else
        {
            wasEnabled = entity.IsEnabled;
        }

        entity.IsEnabled = enabled;
        await _db.SaveChangesAsync(ct);
        await _audit.RecordAsync(ConfigEntityTypes.FeatureFlag, key, wasEnabled.ToString(), enabled.ToString(), _currentUser.UserName, ct);
        _public.Invalidate();
        return Result.Success();
    }
}
