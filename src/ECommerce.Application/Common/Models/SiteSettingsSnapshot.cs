namespace ECommerce.Application.Common.Models;

/// <summary>
/// Strongly-typed view of the <c>SiteSettings</c> key/value table, cached and
/// surfaced to the layout (header, footer, currency switcher).
/// </summary>
public sealed class SiteSettingsSnapshot
{
    public string SiteName { get; init; } = "ShopSphere";
    public string Tagline { get; init; } = string.Empty;
    public string LogoUrl { get; init; } = "/images/logo.svg";

    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string ContactAddress { get; init; } = string.Empty;

    public string? Facebook { get; init; }
    public string? Instagram { get; init; }
    public string? Twitter { get; init; }

    public string BaseCurrency { get; init; } = "USD";
    public IReadOnlyList<string> SupportedCurrencies { get; init; } = new[] { "USD" };
    public decimal BdtRate { get; init; } = 1m;

    public decimal FreeShippingThreshold { get; init; }
    public decimal FlatShippingRate { get; init; }

    public string FaviconUrl { get; init; } = string.Empty;
    public string ThemePrimaryColor { get; init; } = "#2563eb";
    public string ThemeAccentColor { get; init; } = "#ef4444";
    public string ThemeFontFamily { get; init; } = "Inter";
    public string ThemeButtonStyle { get; init; } = "rounded";

    public decimal TaxRatePercent { get; init; }
    public bool PaymentEnableCod { get; init; } = true;
    public bool PaymentEnableOnlineGateway { get; init; } = true;
    public string BusinessHours { get; init; } = string.Empty;

    /// <summary>Raw values for keys not mapped above.</summary>
    public IReadOnlyDictionary<string, string?> Raw { get; init; } =
        new Dictionary<string, string?>();
}
