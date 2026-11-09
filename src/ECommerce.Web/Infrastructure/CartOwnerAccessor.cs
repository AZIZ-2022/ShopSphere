using System.Security.Claims;
using ECommerce.Application.Cart;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Infrastructure;

public interface ICartOwnerAccessor
{
    /// <summary>The current cart owner (logged-in user, or an anonymous browser — cookie is created if needed).</summary>
    CartOwner GetOwner();

    /// <summary>The guest cookie value if present (used to merge the guest cart on login).</summary>
    string? GetAnonymousId();
}

public sealed class CartOwnerAccessor : ICartOwnerAccessor
{
    public const string CookieName = "cart_ref";

    private readonly IHttpContextAccessor _accessor;

    public CartOwnerAccessor(IHttpContextAccessor accessor) => _accessor = accessor;

    public CartOwner GetOwner()
    {
        var http = _accessor.HttpContext;
        if (http is null) return default;

        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && http.User.Identity?.IsAuthenticated == true)
            return CartOwner.ForUser(userId);

        return CartOwner.ForGuest(EnsureAnonymousId(http));
    }

    public string? GetAnonymousId()
    {
        var value = _accessor.HttpContext?.Request.Cookies[CookieName];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string EnsureAnonymousId(HttpContext http)
    {
        var existing = http.Request.Cookies[CookieName];
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var id = Guid.NewGuid().ToString("N");
        http.Response.Cookies.Append(CookieName, id, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            IsEssential = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps
        });
        // Make it visible within the same request too.
        http.Request.Headers.Cookie = http.Request.Headers.Cookie.ToString() is { Length: > 0 } c
            ? $"{c}; {CookieName}={id}"
            : $"{CookieName}={id}";
        return id;
    }
}
