using System.Globalization;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services;

public sealed class SiteSettingsService : ISiteSettingsService
{
    private const string CacheKey = "site-settings-snapshot";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public SiteSettingsService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<SiteSettingsSnapshot> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettingsSnapshot? cached) && cached is not null)
            return cached;

        var map = await _db.SiteSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase, ct);

        var snapshot = Build(map);
        _cache.Set(CacheKey, snapshot, CacheFor);
        return snapshot;
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    private static SiteSettingsSnapshot Build(IReadOnlyDictionary<string, string?> m)
    {
        string S(string key, string fallback = "") =>
            m.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v! : fallback;

        decimal D(string key, decimal fallback = 0m) =>
            m.TryGetValue(key, out var v) && decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d : fallback;

        bool B(string key, bool fallback) =>
            m.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

        var supported = S("Currency.Supported", "USD")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.ToUpperInvariant())
            .Distinct()
            .ToArray();

        return new SiteSettingsSnapshot
        {
            SiteName = S("Site.Name", "ShopSphere"),
            Tagline = S("Site.Tagline"),
            LogoUrl = S("Site.LogoUrl", "/images/logo.svg"),
            ContactEmail = S("Contact.Email"),
            ContactPhone = S("Contact.Phone"),
            ContactAddress = S("Contact.Address"),
            Facebook = S("Social.Facebook"),
            Instagram = S("Social.Instagram"),
            Twitter = S("Social.Twitter"),
            BaseCurrency = S("Currency.Base", "USD").ToUpperInvariant(),
            SupportedCurrencies = supported.Length > 0 ? supported : new[] { "USD" },
            BdtRate = D("Currency.Rate.BDT", 1m),
            FreeShippingThreshold = D("Shipping.FreeThreshold"),
            FlatShippingRate = D("Shipping.FlatRate"),
            FaviconUrl = S("Site.FaviconUrl"),
            ThemePrimaryColor = S("Theme.PrimaryColor", "#2563eb"),
            ThemeAccentColor = S("Theme.AccentColor", "#ef4444"),
            ThemeFontFamily = S("Theme.FontFamily", "Inter"),
            ThemeButtonStyle = S("Theme.ButtonStyle", "rounded"),
            TaxRatePercent = D("Tax.Rate"),
            PaymentEnableCod = B("Payment.EnableCod", true),
            PaymentEnableOnlineGateway = B("Payment.EnableOnlineGateway", true),
            BusinessHours = S("Store.BusinessHours"),
            Raw = m
        };
    }
}
