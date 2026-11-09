using ECommerce.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

/// <summary>
/// Server-to-server notifications from the payment gateway (e.g. SSLCommerz IPN). Anonymous and
/// antiforgery-exempt by necessity — the gateway calls this directly, with no browser session or
/// CSRF token. Trust comes from <see cref="IPaymentService.HandleCallbackAsync"/> re-verifying the
/// payment with the gateway's own API, never from this request body alone.
/// </summary>
[ApiController]
[Route("api/payments")]
public sealed class PaymentsApiController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsApiController(IPaymentService payments) => _payments = payments;

    [HttpPost("ipn")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ipn(CancellationToken ct)
    {
        var data = Request.HasFormContentType
            ? (await Request.ReadFormAsync(ct)).ToDictionary(k => k.Key, v => v.Value.ToString())
            : Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

        await _payments.HandleCallbackAsync(data, ct);

        // The gateway only needs a 200 OK acknowledgement; failures are logged server-side
        // and the customer-facing success/fail redirect is the authoritative path for the UI.
        return Ok();
    }
}
