using System.Security.Claims;
using ECommerce.Application.Cart;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Wishlist;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.ViewComponents;

public sealed class SiteHeaderViewComponent : ViewComponent
{
    private readonly ISiteSettingsService _settings;
    private readonly ICartService _cart;
    private readonly ICartOwnerAccessor _owner;
    private readonly IWishlistService _wishlist;
    private readonly IMoneyFormatter _money;

    public SiteHeaderViewComponent(
        ISiteSettingsService settings,
        ICartService cart,
        ICartOwnerAccessor owner,
        IWishlistService wishlist,
        IMoneyFormatter money)
    {
        _settings = settings;
        _cart = cart;
        _owner = owner;
        _wishlist = wishlist;
        _money = money;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await _settings.GetAsync();
        var currency = CurrencyCookie.Resolve(HttpContext, settings.SupportedCurrencies, settings.BaseCurrency);

        var cart = await _cart.GetCartAsync(_owner.GetOwner());

        var wishlistCount = 0;
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && HttpContext.User.Identity?.IsAuthenticated == true)
            wishlistCount = await _wishlist.CountAsync(userId);

        var vm = new SiteHeaderViewModel
        {
            Settings = settings,
            CurrentCurrency = currency,
            CartCount = cart.ItemCount,
            CartSubTotalDisplay = _money.Format(cart.SubTotal),
            CartTotalDisplay = cart.ItemCount > 0 ? _money.Format(cart.SubTotal) : null,
            WishlistCount = wishlistCount,
            MiniCartLines = cart.Lines.Take(4).Select(l => new MiniCartLine(
                l.ProductId, l.Slug, l.Name, l.Quantity, _money.Format(l.LineTotal), l.ImageUrl)).ToList(),
            MiniCartMore = Math.Max(0, cart.DistinctCount - 4)
        };

        return View(vm);
    }
}

public sealed record MiniCartLine(int ProductId, string Slug, string Name, int Quantity, string LineTotalDisplay, string? ImageUrl);

public sealed class SiteHeaderViewModel
{
    public required SiteSettingsSnapshot Settings { get; init; }
    public required string CurrentCurrency { get; init; }
    public int CartCount { get; init; }
    public string CartSubTotalDisplay { get; init; } = "";
    public string? CartTotalDisplay { get; init; }
    public int WishlistCount { get; init; }
    public IReadOnlyList<MiniCartLine> MiniCartLines { get; init; } = Array.Empty<MiniCartLine>();
    public int MiniCartMore { get; init; }
}
