using System.Security.Claims;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Wishlist;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("wishlist")]
[Authorize]
public sealed class WishlistController : Controller
{
    private readonly IWishlistService _wishlist;
    private readonly IFeatureFlagService _features;

    public WishlistController(IWishlistService wishlist, IFeatureFlagService features)
    {
        _wishlist = wishlist;
        _features = features;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Wishlist, ct)) return NotFound();

        this.SetBreadcrumb(new BreadcrumbItem("My Wishlist"));
        var items = await _wishlist.GetItemsAsync(UserId, ct);
        return View(items);
    }

    [HttpPost("remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Wishlist, ct)) return NotFound();

        await _wishlist.RemoveAsync(UserId, productId, ct);
        TempData["Flash"] = "Removed from wishlist.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("move-to-cart")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveToCart(int productId, CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Wishlist, ct)) return NotFound();

        var result = await _wishlist.MoveToCartAsync(UserId, productId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? "Moved to your cart." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
