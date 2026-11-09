using System.Security.Claims;
using ECommerce.Application.Orders;
using ECommerce.Application.Payments;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using ECommerce.Web.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("order")]
public sealed class OrderController : Controller
{
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;

    public OrderController(IOrderService orders, IPaymentService payments)
    {
        _orders = orders;
        _payments = payments;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("confirmation/{orderNumber}")]
    [Authorize]
    public async Task<IActionResult> Confirmation(string orderNumber, CancellationToken ct)
    {
        var order = await _orders.GetForCustomerAsync(UserId!, orderNumber, ct);
        if (order is null) return NotFound();

        if (order.AwaitingPayment)
            return RedirectToAction(nameof(Pay), new { orderNumber });

        this.SetBreadcrumb(new BreadcrumbItem("Order confirmed"));
        return View(order);
    }

    [HttpGet("pay/{orderNumber}")]
    [Authorize]
    public async Task<IActionResult> Pay(string orderNumber, CancellationToken ct)
    {
        var order = await _payments.GetPayableAsync(UserId!, orderNumber, ct);
        if (order is null) return NotFound();

        if (order.AlreadyPaid)
            return RedirectToAction(nameof(Confirmation), new { orderNumber });

        this.SetBreadcrumb(new BreadcrumbItem("Payment"));

        if (_payments.UsesRedirectCheckout)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var initiate = await _payments.InitiateRedirectAsync(
                UserId!, orderNumber,
                successUrl: $"{baseUrl}/order/pay/{orderNumber}/success",
                failUrl: $"{baseUrl}/order/pay/{orderNumber}/fail",
                cancelUrl: $"{baseUrl}/order/pay/{orderNumber}/cancel",
                ipnUrl: $"{baseUrl}/api/payments/ipn",
                ct);

            if (initiate.Succeeded && !string.IsNullOrWhiteSpace(initiate.Data))
                return Redirect(initiate.Data);

            return View(new PaymentPageModel { Order = order, GatewayError = initiate.Error ?? "Could not start the payment session." });
        }

        return View(new PaymentPageModel { Order = order });
    }

    // SSLCommerz (and similar hosted gateways) redirect the customer's browser back to these
    // three URLs after checkout. We never trust the redirect body itself — HandleCallbackAsync
    // re-verifies with the gateway server-to-server before marking anything paid, so these are
    // intentionally anonymous and antiforgery-exempt (the gateway, not our cookie, is the caller).

    [HttpGet("pay/{orderNumber}/success")]
    [HttpPost("pay/{orderNumber}/success")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PaySuccess(string orderNumber, CancellationToken ct)
    {
        var result = await _payments.HandleCallbackAsync(await ReadCallbackDataAsync(), ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded
            ? "Payment successful — thank you!"
            : (result.Error ?? "We could not confirm your payment. If you were charged, please contact support.");
        return RedirectToAction(nameof(Confirmation), new { orderNumber });
    }

    [HttpGet("pay/{orderNumber}/fail")]
    [HttpPost("pay/{orderNumber}/fail")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PayFail(string orderNumber, CancellationToken ct)
    {
        await _payments.HandleCallbackAsync(await ReadCallbackDataAsync(), ct);
        TempData["FlashError"] = "Your payment was not completed. Please try again.";
        return RedirectToAction(nameof(Pay), new { orderNumber });
    }

    [HttpGet("pay/{orderNumber}/cancel")]
    [HttpPost("pay/{orderNumber}/cancel")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult PayCancel(string orderNumber)
    {
        TempData["FlashError"] = "Payment was cancelled.";
        return RedirectToAction(nameof(Pay), new { orderNumber });
    }

    private async Task<IReadOnlyDictionary<string, string>> ReadCallbackDataAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            return form.ToDictionary(k => k.Key, v => v.Value.ToString());
        }
        return Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
    }

    [HttpPost("pay/{orderNumber}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(string orderNumber, CardForm card, CancellationToken ct)
    {
        var order = await _payments.GetPayableAsync(UserId!, orderNumber, ct);
        if (order is null) return NotFound();
        if (order.AlreadyPaid)
            return RedirectToAction(nameof(Confirmation), new { orderNumber });

        this.SetBreadcrumb(new BreadcrumbItem("Payment"));

        if (ModelState.IsValid)
        {
            var result = await _payments.ProcessAsync(UserId!, orderNumber, new CardInput
            {
                CardHolder = card.CardHolder,
                CardNumber = card.CardNumber,
                Expiry = card.Expiry,
                Cvc = card.Cvc
            }, ct);

            if (result.Succeeded)
            {
                TempData["Flash"] = "Payment successful — thank you!";
                return RedirectToAction(nameof(Confirmation), new { orderNumber });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Payment was declined.");
        }

        return View(new PaymentPageModel { Order = order, Card = card });
    }

    [HttpGet("track")]
    public IActionResult Track()
    {
        this.SetBreadcrumb(new BreadcrumbItem("Track Order"));
        return View(new TrackOrderModel());
    }

    [HttpPost("track")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(TrackOrderModel model, CancellationToken ct)
    {
        this.SetBreadcrumb(new BreadcrumbItem("Track Order"));
        model.Searched = true;

        if (ModelState.IsValid)
        {
            model.Result = await _orders.TrackAsync(model.OrderNumber.Trim(), model.Email.Trim(), ct);
            if (model.Result is null)
                ModelState.AddModelError(string.Empty, "We couldn't find an order matching that number and email.");
        }

        return View(model);
    }
}
