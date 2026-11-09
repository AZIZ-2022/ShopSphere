namespace ECommerce.Web.Infrastructure;

/// <summary>
/// Maps the small, fixed set of admin-choosable theme options (font, button style, colour
/// presets) to the actual CSS needed to render them. Keeping this as a closed set — rather than
/// letting admins type arbitrary font names or load arbitrary external stylesheets — avoids
/// broken layouts and unvetted third-party resources.
/// </summary>
public static class ThemeCatalog
{
    public static readonly (string Key, string Label)[] Fonts =
    {
        ("Inter", "Inter (default)"),
        ("Roboto", "Roboto"),
        ("Poppins", "Poppins"),
        ("Nunito", "Nunito"),
        ("OpenSans", "Open Sans"),
    };

    public static readonly (string Key, string Label)[] ButtonStyles =
    {
        ("rounded", "Rounded (default)"),
        ("pill", "Pill"),
        ("square", "Square"),
    };

    public static readonly (string Key, string Label, string Primary, string Accent)[] Presets =
    {
        ("blue", "Default blue", "#2563eb", "#ef4444"),
        ("emerald", "Emerald", "#059669", "#f59e0b"),
        ("violet", "Violet", "#7c3aed", "#ec4899"),
        ("slate", "Slate", "#334155", "#dc2626"),
    };

    public static string GoogleFontsHref(string fontKey) => fontKey switch
    {
        "Roboto" => "https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;600;700;800&display=swap",
        "Poppins" => "https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700;800&display=swap",
        "Nunito" => "https://fonts.googleapis.com/css2?family=Nunito:wght@400;500;600;700;800&display=swap",
        "OpenSans" => "https://fonts.googleapis.com/css2?family=Open+Sans:wght@400;500;600;700;800&display=swap",
        _ => "https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap",
    };

    public static string FontStack(string fontKey) => fontKey switch
    {
        "Roboto" => "'Roboto', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
        "Poppins" => "'Poppins', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
        "Nunito" => "'Nunito', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
        "OpenSans" => "'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
        _ => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
    };

    public static string ButtonRadius(string styleKey) => styleKey switch
    {
        "pill" => "999px",
        "square" => "4px",
        _ => "8px",
    };

    /// <summary>True when the stored value looks like a real, usable hex colour.</summary>
    public static bool IsValidHex(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$");

    /// <summary>Darkens a hex colour for hover states — admins only pick one shade per colour.</summary>
    public static string Darken(string hex, double amount = 0.16) => Shift(hex, -amount);

    /// <summary>Lightens a hex colour toward white, for soft/tint backgrounds.</summary>
    public static string Soften(string hex, double amount = 0.9) => Shift(hex, amount);

    private static string Shift(string hex, double amount)
    {
        if (!IsValidHex(hex)) return hex;

        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);

        int Mix(int channel) => amount >= 0
            ? (int)Math.Round(channel + (255 - channel) * amount)
            : (int)Math.Round(channel * (1 + amount));

        r = Math.Clamp(Mix(r), 0, 255);
        g = Math.Clamp(Mix(g), 0, 255);
        b = Math.Clamp(Mix(b), 0, 255);

        return $"#{r:x2}{g:x2}{b:x2}";
    }
}
