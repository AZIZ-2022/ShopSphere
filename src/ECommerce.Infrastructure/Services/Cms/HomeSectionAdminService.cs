using System.Text.Json;
using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Home;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Cms;

public sealed class HomeSectionAdminService : IHomeSectionAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IHomePageService _home;
    private readonly IConfigAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public HomeSectionAdminService(ApplicationDbContext db, IHomePageService home, IConfigAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _home = home;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<HomeSectionEditModel>> ListAsync(CancellationToken ct = default)
    {
        var sections = await _db.HomeSections.AsNoTracking().OrderBy(s => s.DisplayOrder).ToListAsync(ct);
        return sections.Select(Map).ToList();
    }

    public async Task<HomeSectionEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.HomeSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return s is null ? null : Map(s);
    }

    public async Task<Result> UpdateAsync(HomeSectionEditModel model, CancellationToken ct = default)
    {
        var s = await _db.HomeSections.FirstOrDefaultAsync(x => x.Id == model.Id, ct);
        if (s is null) return Result.Failure("Section not found.");

        var before = JsonSerializer.Serialize(Map(s));

        s.Title = Trim(model.Title);
        s.Subtitle = Trim(model.Subtitle);
        s.ButtonText = Trim(model.ButtonText);
        s.LinkUrl = Trim(model.LinkUrl);

        if (string.Equals(s.Key, HomeSectionKeys.FeatureStrip, StringComparison.OrdinalIgnoreCase))
        {
            var items = (model.FeatureItems ?? new())
                .Where(i => !string.IsNullOrWhiteSpace(i.Title))
                .Take(6)
                .ToList();
            s.ConfigJson = items.Count > 0 ? JsonSerializer.Serialize(items) : null;
        }

        var after = JsonSerializer.Serialize(Map(s));

        await _db.SaveChangesAsync(ct);
        await _audit.RecordAsync(ConfigEntityTypes.HomeSection, s.Id.ToString(), before, after, _currentUser.UserName, ct);
        _home.Invalidate();
        return Result.Success();
    }

    public async Task<Result> MoveAsync(int id, bool up, CancellationToken ct = default)
    {
        var all = await _db.HomeSections.OrderBy(s => s.DisplayOrder).ToListAsync(ct);
        var index = all.FindIndex(s => s.Id == id);
        if (index < 0) return Result.Failure("Section not found.");

        var swapWith = up ? index - 1 : index + 1;
        if (swapWith < 0 || swapWith >= all.Count) return Result.Success(); // already at the edge

        (all[index].DisplayOrder, all[swapWith].DisplayOrder) = (all[swapWith].DisplayOrder, all[index].DisplayOrder);
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result.Success();
    }

    public async Task<Result> SetVisibleAsync(int id, bool visible, CancellationToken ct = default)
    {
        var s = await _db.HomeSections.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return Result.Failure("Section not found.");

        s.IsVisible = visible;
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result.Success();
    }

    private static HomeSectionEditModel Map(HomeSection s)
    {
        var label = HomeSectionKeys.All.FirstOrDefault(x => x.Key == s.Key).Label ?? s.Key;

        List<FeatureStripItem> items = new();
        if (!string.IsNullOrWhiteSpace(s.ConfigJson))
        {
            try { items = JsonSerializer.Deserialize<List<FeatureStripItem>>(s.ConfigJson) ?? new(); }
            catch (JsonException) { /* malformed config — admin can re-enter the items */ }
        }

        return new HomeSectionEditModel
        {
            Id = s.Id,
            Key = s.Key,
            Label = label,
            DisplayOrder = s.DisplayOrder,
            IsVisible = s.IsVisible,
            Title = s.Title,
            Subtitle = s.Subtitle,
            ButtonText = s.ButtonText,
            LinkUrl = s.LinkUrl,
            FeatureItems = items
        };
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
