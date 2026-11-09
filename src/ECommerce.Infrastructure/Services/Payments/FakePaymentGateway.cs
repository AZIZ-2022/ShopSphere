using ECommerce.Application.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payments;

/// <summary>
/// Simulated payment gateway for the demo. Behaviour is configurable via the
/// <c>Payment:Fake</c> section of appsettings:
///   SimulateSuccess    — global switch (default true)
///   DeclineCardSuffix  — any card number ending with this is always declined (default "0000")
/// A basic Luhn check is applied so obviously bogus numbers fail.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly bool _simulateSuccess;
    private readonly string _declineSuffix;
    private readonly ILogger<FakePaymentGateway> _logger;

    public FakePaymentGateway(IConfiguration config, ILogger<FakePaymentGateway> logger)
    {
        _simulateSuccess = config.GetValue("Payment:Fake:SimulateSuccess", true);
        _declineSuffix = config.GetValue("Payment:Fake:DeclineCardSuffix", "0000") ?? "0000";
        _logger = logger;
    }

    public string Name => "FakeGateway";
    public bool RequiresRedirect => false;

    public Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException("The fake gateway uses an on-site card form — call ChargeAsync instead.");

    public Task<GatewayCallbackResult> ValidateCallbackAsync(IReadOnlyDictionary<string, string> callback, CancellationToken ct = default) =>
        throw new NotSupportedException("The fake gateway uses an on-site card form — it has no redirect callback.");

    public async Task<GatewayResult> ChargeAsync(GatewayChargeRequest request, CancellationToken ct = default)
    {
        await Task.Delay(400, ct); // pretend we're calling a remote API

        var digits = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length is < 12 or > 19 || !PassesLuhn(digits))
            return GatewayResult.Declined("The card number is invalid.", "fake:invalid_number");

        if (string.IsNullOrWhiteSpace(request.Cvc) || request.Cvc.Length is < 3 or > 4)
            return GatewayResult.Declined("The security code is invalid.", "fake:invalid_cvc");

        if (!_simulateSuccess || digits.EndsWith(_declineSuffix, StringComparison.Ordinal))
        {
            _logger.LogInformation("Fake gateway declined charge for {Order}.", request.OrderNumber);
            return GatewayResult.Declined("Your card was declined. Please try a different card.", "fake:declined");
        }

        var txnId = $"FAKE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        _logger.LogInformation("Fake gateway approved {Txn} for {Order}.", txnId, request.OrderNumber);
        return GatewayResult.Ok(txnId, $"fake:approved amount={request.Amount:0.00} {request.CurrencyCode}");
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var alt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (alt)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            alt = !alt;
        }
        return sum % 10 == 0;
    }
}
