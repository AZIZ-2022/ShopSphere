using System.Globalization;
using System.Text.Json;
using ECommerce.Application.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payments;

/// <summary>
/// SSLCommerz hosted-checkout gateway. The customer is redirected to SSLCommerz's own payment
/// page (<see cref="InitiateAsync"/>); we never see card data. A returning success/fail/cancel
/// callback or IPN is only trusted after <see cref="ValidateCallbackAsync"/> confirms it
/// server-to-server against SSLCommerz's validation API — the redirect/POST body itself is
/// never trusted on its own.
///
/// Configure via appsettings: Payment:SslCommerz:StoreId / StorePassword / Sandbox.
/// Sandbox credentials: https://developer.sslcommerz.com (merchant sandbox registration).
/// </summary>
public sealed class SslCommerzPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly string _storeId;
    private readonly string _storePassword;
    private readonly string _baseUrl;
    private readonly ILogger<SslCommerzPaymentGateway> _logger;

    public SslCommerzPaymentGateway(HttpClient http, IConfiguration config, ILogger<SslCommerzPaymentGateway> logger)
    {
        _http = http;
        _storeId = config["Payment:SslCommerz:StoreId"] ?? string.Empty;
        _storePassword = config["Payment:SslCommerz:StorePassword"] ?? string.Empty;
        var sandbox = config.GetValue("Payment:SslCommerz:Sandbox", true);
        _baseUrl = sandbox ? "https://sandbox.sslcommerz.com" : "https://securepay.sslcommerz.com";
        _logger = logger;
    }

    public string Name => "SSLCommerz";
    public bool RequiresRedirect => true;

    public Task<GatewayResult> ChargeAsync(GatewayChargeRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException("SSLCommerz uses hosted redirect checkout — call InitiateAsync instead.");

    public async Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_storeId) || string.IsNullOrWhiteSpace(_storePassword))
        {
            _logger.LogError("SSLCommerz is selected as the payment gateway but Payment:SslCommerz:StoreId/StorePassword are not configured.");
            return GatewayInitiateResult.Fail("Online payment is not configured yet. Please choose Cash on Delivery, or contact support.");
        }

        var form = new Dictionary<string, string>
        {
            ["store_id"] = _storeId,
            ["store_passwd"] = _storePassword,
            ["total_amount"] = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = request.CurrencyCode,
            ["tran_id"] = request.OrderNumber,
            ["success_url"] = request.SuccessUrl,
            ["fail_url"] = request.FailUrl,
            ["cancel_url"] = request.CancelUrl,
            ["ipn_url"] = request.IpnUrl,
            ["shipping_method"] = "NO",
            ["product_name"] = $"Order {request.OrderNumber}",
            ["product_category"] = "General",
            ["product_profile"] = "general",
            ["cus_name"] = string.IsNullOrWhiteSpace(request.CustomerName) ? "Customer" : request.CustomerName,
            ["cus_email"] = string.IsNullOrWhiteSpace(request.CustomerEmail) ? "customer@example.com" : request.CustomerEmail,
            ["cus_add1"] = string.IsNullOrWhiteSpace(request.CustomerAddress) ? "N/A" : request.CustomerAddress,
            ["cus_city"] = string.IsNullOrWhiteSpace(request.CustomerCity) ? "N/A" : request.CustomerCity,
            ["cus_country"] = string.IsNullOrWhiteSpace(request.CustomerCountry) ? "Bangladesh" : request.CustomerCountry,
            ["cus_phone"] = string.IsNullOrWhiteSpace(request.CustomerPhone) ? "00000000000" : request.CustomerPhone,
        };

        string raw;
        HttpResponseMessage response;
        try
        {
            using var content = new FormUrlEncodedContent(form);
            response = await _http.PostAsync($"{_baseUrl}/gwprocess/v4/api.php", content, ct);
            raw = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "SSLCommerz initiate request failed.");
            return GatewayInitiateResult.Fail("Could not reach the payment gateway. Please try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SSLCommerz initiate returned {Status}: {Body}", response.StatusCode, raw);
            return GatewayInitiateResult.Fail("Could not start the payment session. Please try again.");
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;

        if (!string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            var reason = root.TryGetProperty("failedreason", out var fr) ? fr.GetString() : null;
            _logger.LogWarning("SSLCommerz initiate rejected ({Status}): {Reason}", status, reason);
            return GatewayInitiateResult.Fail(string.IsNullOrWhiteSpace(reason) ? "Could not start the payment session." : reason);
        }

        var url = root.TryGetProperty("GatewayPageURL", out var u) ? u.GetString() : null;
        return string.IsNullOrWhiteSpace(url)
            ? GatewayInitiateResult.Fail("The gateway did not return a payment page.")
            : GatewayInitiateResult.Ok(url);
    }

    public async Task<GatewayCallbackResult> ValidateCallbackAsync(IReadOnlyDictionary<string, string> callback, CancellationToken ct = default)
    {
        callback.TryGetValue("val_id", out var valId);
        callback.TryGetValue("tran_id", out var fallbackTranId);

        if (string.IsNullOrWhiteSpace(valId))
        {
            return new GatewayCallbackResult
            {
                Verified = false,
                PaymentSucceeded = false,
                OrderNumber = fallbackTranId,
                RawResponse = "(no val_id in callback)",
                FailureReason = "The payment gateway callback was missing its validation id."
            };
        }

        var query = $"val_id={Uri.EscapeDataString(valId)}" +
                    $"&store_id={Uri.EscapeDataString(_storeId)}" +
                    $"&store_passwd={Uri.EscapeDataString(_storePassword)}" +
                    "&format=json";

        string raw;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync($"{_baseUrl}/validator/api/validationserverAPI.php?{query}", ct);
            raw = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "SSLCommerz validation request failed for val_id {ValId}.", valId);
            return new GatewayCallbackResult
            {
                Verified = false,
                PaymentSucceeded = false,
                OrderNumber = fallbackTranId,
                RawResponse = ex.Message,
                FailureReason = "Could not reach the payment gateway to verify this payment."
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SSLCommerz validation call returned {Status}: {Body}", response.StatusCode, raw);
            return new GatewayCallbackResult
            {
                Verified = false,
                PaymentSucceeded = false,
                OrderNumber = fallbackTranId,
                RawResponse = raw,
                FailureReason = "Could not verify this payment with the gateway."
            };
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
        var tranId = root.TryGetProperty("tran_id", out var t) ? t.GetString() : fallbackTranId;
        var amountStr = root.TryGetProperty("amount", out var a) ? a.GetString() : null;
        var bankTxnId = root.TryGetProperty("bank_tran_id", out var b) ? b.GetString() : valId;

        var success = string.Equals(status, "VALID", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(status, "VALIDATED", StringComparison.OrdinalIgnoreCase);

        decimal? amount = decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

        return new GatewayCallbackResult
        {
            Verified = true, // we got an authoritative answer from SSLCommerz's own server
            PaymentSucceeded = success,
            OrderNumber = tranId,
            TransactionId = bankTxnId,
            Amount = amount,
            RawResponse = raw,
            FailureReason = success ? null : $"Payment could not be validated (gateway status: {status})."
        };
    }
}
