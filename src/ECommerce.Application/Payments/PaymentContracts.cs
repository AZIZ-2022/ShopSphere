using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Payments;

/// <summary>Request handed to a payment gateway to authorise a charge.</summary>
public sealed class GatewayChargeRequest
{
    public string OrderNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }          // base currency (USD)
    public string CurrencyCode { get; init; } = "USD";
    public string CardHolder { get; init; } = string.Empty;
    public string CardNumber { get; init; } = string.Empty;   // digits only, mock
    public string Expiry { get; init; } = string.Empty;       // MM/YY
    public string Cvc { get; init; } = string.Empty;
}

public sealed class GatewayResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string RawResponse { get; init; } = string.Empty;
    public string? FailureReason { get; init; }

    public static GatewayResult Ok(string txnId, string raw) =>
        new() { Success = true, TransactionId = txnId, RawResponse = raw };

    public static GatewayResult Declined(string reason, string raw) =>
        new() { Success = false, FailureReason = reason, RawResponse = raw };
}

/// <summary>Request to start a hosted/redirect checkout session (SSLCommerz, Stripe Checkout, …).</summary>
public sealed class GatewayInitiateRequest
{
    public string OrderNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }           // base currency
    public string CurrencyCode { get; init; } = "USD";
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string CustomerAddress { get; init; } = string.Empty;
    public string CustomerCity { get; init; } = string.Empty;
    public string CustomerCountry { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string FailUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
    public string IpnUrl { get; init; } = string.Empty;
}

public sealed class GatewayInitiateResult
{
    public bool Success { get; init; }
    public string? RedirectUrl { get; init; }
    public string? FailureReason { get; init; }

    public static GatewayInitiateResult Ok(string url) => new() { Success = true, RedirectUrl = url };
    public static GatewayInitiateResult Fail(string reason) => new() { Success = false, FailureReason = reason };
}

/// <summary>
/// Result of verifying a gateway callback/IPN server-to-server. <see cref="Verified"/> is true
/// when we got an authoritative answer from the gateway's own API (regardless of whether the
/// payment itself succeeded) — callers must never trust the redirect/POST body alone.
/// </summary>
public sealed class GatewayCallbackResult
{
    public bool Verified { get; init; }
    public bool PaymentSucceeded { get; init; }
    public string? OrderNumber { get; init; }
    public string? TransactionId { get; init; }
    public decimal? Amount { get; init; }
    public string RawResponse { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
}

/// <summary>
/// Payment gateway abstraction. The shipped default is a fake/simulated direct-charge gateway;
/// set <c>Payment:Gateway</c> to a real provider name to swap it in without touching the order flow.
/// A gateway implements either the direct-charge members (<see cref="ChargeAsync"/>, when
/// <see cref="RequiresRedirect"/> is false) or the hosted-checkout members (<see cref="InitiateAsync"/>/
/// <see cref="ValidateCallbackAsync"/>, when <see cref="RequiresRedirect"/> is true) — never both.
/// </summary>
public interface IPaymentGateway
{
    string Name { get; }

    /// <summary>True for hosted/redirect gateways (SSLCommerz, Stripe Checkout) where the customer
    /// completes payment on the provider's own page; false for direct-charge gateways (the Fake gateway).</summary>
    bool RequiresRedirect { get; }

    Task<GatewayResult> ChargeAsync(GatewayChargeRequest request, CancellationToken ct = default);
    Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest request, CancellationToken ct = default);
    Task<GatewayCallbackResult> ValidateCallbackAsync(IReadOnlyDictionary<string, string> callback, CancellationToken ct = default);
}

// --------------------------------------------------------------- Payment flow

public sealed class PayableOrderDto
{
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }        // base currency
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public PaymentStatus PaymentStatus { get; init; }
    public PaymentMethodType PaymentMethod { get; init; }
    public bool AlreadyPaid => PaymentStatus == PaymentStatus.Paid;
    public bool NeedsPayment => PaymentMethod == PaymentMethodType.OnlineGateway
                                && PaymentStatus is PaymentStatus.Pending or PaymentStatus.Failed;
}

public sealed class CardInput
{
    public string CardHolder { get; init; } = string.Empty;
    public string CardNumber { get; init; } = string.Empty;
    public string Expiry { get; init; } = string.Empty;
    public string Cvc { get; init; } = string.Empty;
}

public interface IPaymentService
{
    Task<PayableOrderDto?> GetPayableAsync(string userId, string orderNumber, CancellationToken ct = default);

    /// <summary>True when the configured gateway uses hosted/redirect checkout rather than an on-site card form.</summary>
    bool UsesRedirectCheckout { get; }

    /// <summary>Direct-charge flow (Fake gateway only) — processes a card submitted on our own page.</summary>
    Task<Result> ProcessAsync(string userId, string orderNumber, CardInput card, CancellationToken ct = default);

    /// <summary>Hosted-checkout flow — starts a gateway session and returns the URL to redirect the browser to.</summary>
    Task<Result<string>> InitiateRedirectAsync(
        string userId, string orderNumber,
        string successUrl, string failUrl, string cancelUrl, string ipnUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies a gateway callback/IPN server-to-server and applies it to the matching order.
    /// Returns the order number on success. Safe to call more than once for the same payment.
    /// </summary>
    Task<Result<string>> HandleCallbackAsync(IReadOnlyDictionary<string, string> callback, CancellationToken ct = default);
}
