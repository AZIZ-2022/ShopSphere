using System.Security.Claims;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using ECommerce.Web.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("checkout")]
[Authorize]
public sealed class CheckoutController : Controller
{
    private readonly IOrderService _orders;
    private readonly ISiteSettingsService _settings;

    public CheckoutController(IOrderService orders, ISiteSettingsService settings)
    {
        _orders = orders;
        _settings = settings;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? coupon, CancellationToken ct)
    {
        var context = await _orders.BuildCheckoutAsync(UserId, coupon, ct);
        if (context.IsEmpty)
        {
            TempData["FlashError"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        this.SetBreadcrumb(new BreadcrumbItem("Cart", "/cart"), new BreadcrumbItem("Checkout"));

        var defaultAddress = context.Addresses.FirstOrDefault(a => a.IsDefault) ?? context.Addresses.FirstOrDefault();
        return View(new CheckoutViewModel
        {
            Context = context,
            Form = new CheckoutForm
            {
                ShippingAddressId = defaultAddress?.Id,
                CouponCode = context.AppliedCoupon ?? coupon
            },
            CouponApplied = context.AppliedCoupon is not null,
            CouponMessage = context.AppliedCoupon is not null ? $"Coupon “{context.AppliedCoupon}” applied." : null
        });
    }

    [HttpPost("apply-coupon")]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyCoupon(string? couponCode) =>
        RedirectToAction(nameof(Index), new { coupon = couponCode });

    [HttpPost("place")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(CheckoutForm form, CancellationToken ct)
    {
        this.SetBreadcrumb(new BreadcrumbItem("Cart", "/cart"), new BreadcrumbItem("Checkout"));

        var usingNew = form.ShippingAddressId is null or 0;
        if (usingNew)
        {
            // Validate only the new-address sub-form.
            var addrErrors = new ValidationContextValidator().Validate(form.NewAddress);
            foreach (var (key, msg) in addrErrors)
                ModelState.AddModelError($"Form.NewAddress.{key}", msg);
        }
        else
        {
            // Ignore new-address validation errors when a saved address is chosen.
            foreach (var key in ModelState.Keys.Where(k => k.Contains("NewAddress")).ToList())
                ModelState.Remove(key);
        }

        var settings = await _settings.GetAsync(ct);
        var currency = CurrencyCookie.Resolve(HttpContext, settings.SupportedCurrencies, settings.BaseCurrency);
        var rate = string.Equals(currency, "BDT", StringComparison.OrdinalIgnoreCase) ? settings.BdtRate : 1m;

        if (ModelState.IsValid)
        {
            var result = await _orders.PlaceOrderAsync(UserId, new PlaceOrderRequest
            {
                ShippingAddressId = usingNew ? null : form.ShippingAddressId,
                NewAddress = usingNew ? form.NewAddress : null,
                PaymentMethod = form.PaymentMethod,
                CouponCode = form.CouponCode,
                CustomerNote = form.CustomerNote,
                CurrencyCode = currency,
                ExchangeRate = rate
            }, ct);

            if (result.Succeeded)
            {
                var placed = result.Data!;
                return placed.RequiresPayment
                    ? RedirectToAction("Pay", "Order", new { orderNumber = placed.OrderNumber })
                    : RedirectToAction("Confirmation", "Order", new { orderNumber = placed.OrderNumber });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        var context = await _orders.BuildCheckoutAsync(UserId, form.CouponCode, ct);
        if (context.IsEmpty)
            return RedirectToAction("Index", "Cart");

        return View(nameof(Index), new CheckoutViewModel
        {
            Context = context,
            Form = form,
            CouponApplied = context.AppliedCoupon is not null,
            CouponMessage = context.AppliedCoupon is not null ? $"Coupon “{context.AppliedCoupon}” applied." : null
        });
    }
}

/// <summary>Runs DataAnnotations validation on a POCO and returns member/message pairs.</summary>
internal sealed class ValidationContextValidator
{
    public IReadOnlyList<(string Member, string Message)> Validate(object instance)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            instance, new System.ComponentModel.DataAnnotations.ValidationContext(instance), results, validateAllProperties: true);

        return results
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : new[] { "" })
                .Select(m => (m, r.ErrorMessage ?? "Invalid value.")))
            .ToList();
    }
}
