using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Infrastructure;

/// <summary>Reads/writes the visitor's selected display currency (Phase 2 stores only the choice).</summary>
public static class CurrencyCookie
{
    public const string Name = "currency";

    public static string Resolve(HttpContext http, IReadOnlyList<string> supported, string fallback)
    {
        var value = http.Request.Cookies[Name];
        if (!string.IsNullOrWhiteSpace(value))
        {
            var upper = value.ToUpperInvariant();
            if (supported.Contains(upper))
                return upper;
        }
        return fallback;
    }

    public static void Write(HttpContext http, string code)
    {
        http.Response.Cookies.Append(Name, code.ToUpperInvariant(), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps
        });
    }
}
