using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Payments;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IDateTimeProvider _clock;

    public PaymentService(ApplicationDbContext db, IPaymentGateway gateway, IDateTimeProvider clock)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
    }

    public bool UsesRedirectCheckout => _gateway.RequiresRedirect;

    public async Task<PayableOrderDto?> GetPayableAsync(string userId, string orderNumber, CancellationToken ct = default)
    {
        return await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.OrderNumber == orderNumber)
            .Select(o => new PayableOrderDto
            {
                OrderNumber = o.OrderNumber,
                TotalAmount = o.TotalAmount,
                CurrencyCode = o.CurrencyCode,
                ExchangeRate = o.ExchangeRate,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result> ProcessAsync(string userId, string orderNumber, CardInput card, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct);

        if (order is null)
            return Result.Failure("Order not found.");
        if (order.PaymentMethod != PaymentMethodType.OnlineGateway)
            return Result.Failure("This order does not require online payment.");
        if (order.PaymentStatus == PaymentStatus.Paid)
            return Result.Success();

        var now = _clock.UtcNow;
        var result = await _gateway.ChargeAsync(new GatewayChargeRequest
        {
            OrderNumber = order.OrderNumber,
            Amount = order.TotalAmount,
            CurrencyCode = order.CurrencyCode,
            CardHolder = card.CardHolder,
            CardNumber = card.CardNumber,
            Expiry = card.Expiry,
            Cvc = card.Cvc
        }, ct);

        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        if (payment is null)
        {
            payment = new Payment { Method = PaymentMethodType.OnlineGateway, Amount = order.TotalAmount, CreatedAt = now };
            order.Payments.Add(payment);
        }

        if (result.Success)
        {
            payment.Status = PaymentStatus.Paid;
            payment.TransactionId = result.TransactionId;
            payment.GatewayResponse = result.RawResponse;
            payment.PaidAt = now;

            order.PaymentStatus = PaymentStatus.Paid;
            if (order.Status == OrderStatus.Pending)
                order.Status = OrderStatus.Confirmed;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = order.Status,
                Note = $"Online payment received ({result.TransactionId}).",
                ChangedAt = now,
                ChangedBy = userId
            });

            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        payment.Status = PaymentStatus.Failed;
        payment.GatewayResponse = result.RawResponse;
        order.PaymentStatus = PaymentStatus.Failed;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.Status,
            Note = $"Payment failed: {result.FailureReason}",
            ChangedAt = now,
            ChangedBy = userId
        });

        await _db.SaveChangesAsync(ct);
        return Result.Failure(result.FailureReason ?? "Payment was declined.");
    }

    public async Task<Result<string>> InitiateRedirectAsync(
        string userId, string orderNumber,
        string successUrl, string failUrl, string cancelUrl, string ipnUrl,
        CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct);
        if (order is null) return Result<string>.Failure("Order not found.");
        if (order.PaymentMethod != PaymentMethodType.OnlineGateway)
            return Result<string>.Failure("This order does not require online payment.");
        if (order.PaymentStatus == PaymentStatus.Paid)
            return Result<string>.Failure("This order is already paid.");

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(ct);

        var initiate = await _gateway.InitiateAsync(new GatewayInitiateRequest
        {
            OrderNumber = order.OrderNumber,
            Amount = order.TotalAmount,
            CurrencyCode = order.CurrencyCode,
            CustomerName = user?.FullName ?? order.ShippingName,
            CustomerEmail = user?.Email ?? string.Empty,
            CustomerPhone = order.ShippingPhone,
            CustomerAddress = order.ShippingLine1,
            CustomerCity = order.ShippingCity,
            CustomerCountry = order.ShippingCountry,
            SuccessUrl = successUrl,
            FailUrl = failUrl,
            CancelUrl = cancelUrl,
            IpnUrl = ipnUrl
        }, ct);

        return initiate.Success && !string.IsNullOrWhiteSpace(initiate.RedirectUrl)
            ? Result<string>.Success(initiate.RedirectUrl)
            : Result<string>.Failure(initiate.FailureReason ?? "Could not start the payment session.");
    }

    public async Task<Result<string>> HandleCallbackAsync(IReadOnlyDictionary<string, string> callback, CancellationToken ct = default)
    {
        var validation = await _gateway.ValidateCallbackAsync(callback, ct);
        if (string.IsNullOrWhiteSpace(validation.OrderNumber))
            return Result<string>.Failure("The payment gateway did not identify an order.");

        var order = await _db.Orders
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderNumber == validation.OrderNumber, ct);
        if (order is null) return Result<string>.Failure("Order not found.");

        // Idempotent: a repeat IPN/callback for an already-settled payment is a no-op success.
        if (order.PaymentStatus == PaymentStatus.Paid)
            return Result<string>.Success(order.OrderNumber);

        if (!validation.Verified)
            return Result<string>.Failure(validation.FailureReason ?? "Could not verify this payment with the gateway.");

        var now = _clock.UtcNow;
        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        if (payment is null)
        {
            payment = new Payment { Method = PaymentMethodType.OnlineGateway, Amount = order.TotalAmount, CreatedAt = now };
            order.Payments.Add(payment);
        }

        if (validation.PaymentSucceeded)
        {
            // Defence in depth: a verified-but-mismatched amount is rejected rather than trusted.
            if (validation.Amount is { } paidAmount && Math.Abs(paidAmount - order.TotalAmount) > 0.01m)
            {
                payment.Status = PaymentStatus.Failed;
                payment.GatewayResponse = validation.RawResponse;
                order.PaymentStatus = PaymentStatus.Failed;
                order.StatusHistory.Add(new OrderStatusHistory { Status = order.Status, Note = "Payment amount did not match the order total — rejected.", ChangedAt = now });
                await _db.SaveChangesAsync(ct);
                return Result<string>.Failure("Payment amount did not match the order total.");
            }

            payment.Status = PaymentStatus.Paid;
            payment.TransactionId = validation.TransactionId;
            payment.GatewayResponse = validation.RawResponse;
            payment.PaidAt = now;

            order.PaymentStatus = PaymentStatus.Paid;
            if (order.Status == OrderStatus.Pending)
                order.Status = OrderStatus.Confirmed;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = order.Status,
                Note = $"Online payment received via gateway ({validation.TransactionId}).",
                ChangedAt = now
            });

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success(order.OrderNumber);
        }

        payment.Status = PaymentStatus.Failed;
        payment.GatewayResponse = validation.RawResponse;
        order.PaymentStatus = PaymentStatus.Failed;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.Status,
            Note = $"Payment failed: {validation.FailureReason}",
            ChangedAt = now
        });

        await _db.SaveChangesAsync(ct);
        return Result<string>.Failure(validation.FailureReason ?? "Payment was declined.");
    }
}
