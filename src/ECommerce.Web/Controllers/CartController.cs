using ECommerce.Application.Cart;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("cart")]
public sealed class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly ICartOwnerAccessor _owner;

    public CartController(ICartService cart, ICartOwnerAccessor owner)
    {
        _cart = cart;
        _owner = owner;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        this.SetBreadcrumb(new BreadcrumbItem("Shopping Cart"));
        var cart = await _cart.GetCartAsync(_owner.GetOwner(), ct);
        return View(cart);
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity, CancellationToken ct)
    {
        var result = await _cart.UpdateQuantityAsync(_owner.GetOwner(), productId, quantity, ct);
        if (!result.Succeeded) TempData["FlashError"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        await _cart.RemoveAsync(_owner.GetOwner(), productId, ct);
        TempData["Flash"] = "Item removed from your cart.";
        return RedirectToAction(nameof(Index));
    }
}
