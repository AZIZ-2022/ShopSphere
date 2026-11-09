using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders;

// ------------------------------------------------------------------- Coupons

public sealed class CouponResult
{
    public bool Valid { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public string? Message { get; init; }
}

public interface ICouponService
{
    /// <summary>Validates a coupon against a base-currency subtotal and returns the discount it would give.</summary>
    Task<CouponResult> ValidateAsync(string code, decimal subTotal, CancellationToken ct = default);
}

// ------------------------------------------------------------ Coupon admin

public sealed class CouponListItem
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public int? UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CouponEditModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartsAt { get; set; } = DateTime.UtcNow.Date;
    public DateTime EndsAt { get; set; } = DateTime.UtcNow.Date.AddMonths(1);
    public bool IsActive { get; set; } = true;
}

public interface ICouponAdminService
{
    Task<IReadOnlyList<CouponListItem>> ListAsync(CancellationToken ct = default);
    Task<CouponEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CouponEditModel model, CancellationToken ct = default);
    Task<Result> UpdateAsync(CouponEditModel model, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

// ------------------------------------------------------------------ Checkout

public sealed class CheckoutLineDto
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class CheckoutContextDto
{
    public IReadOnlyList<CheckoutLineDto> Lines { get; init; } = Array.Empty<CheckoutLineDto>();
    public IReadOnlyList<AddressDto> Addresses { get; init; } = Array.Empty<AddressDto>();

    public decimal SubTotal => Lines.Sum(l => l.LineTotal);
    public decimal ShippingCost { get; init; }
    public decimal FreeShippingThreshold { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string? AppliedCoupon { get; init; }
    public decimal GrandTotal => Math.Max(0, SubTotal - DiscountAmount) + ShippingCost + TaxAmount;

    public bool EnableCod { get; init; } = true;
    public bool EnableOnlineGateway { get; init; } = true;

    public bool IsEmpty => Lines.Count == 0;
}

public sealed class PlaceOrderRequest
{
    public int? ShippingAddressId { get; init; }
    public AddressEditModel? NewAddress { get; init; }
    public PaymentMethodType PaymentMethod { get; init; } = PaymentMethodType.CashOnDelivery;
    public string? CouponCode { get; init; }
    public string? CustomerNote { get; init; }

    /// <summary>Display currency + rate captured from the storefront at checkout time.</summary>
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
}

public sealed class PlaceOrderResultDto
{
    public string OrderNumber { get; init; } = string.Empty;
    public PaymentMethodType PaymentMethod { get; init; }
    public PaymentStatus PaymentStatus { get; init; }

    /// <summary>True when the customer must still complete an online payment.</summary>
    public bool RequiresPayment { get; init; }
}

// -------------------------------------------------------------------- Orders

public sealed class OrderItemDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? Slug { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class OrderStatusEntryDto
{
    public OrderStatus Status { get; init; }
    public string? Note { get; init; }
    public DateTime ChangedAt { get; init; }
}

public sealed class OrderPaymentDto
{
    public PaymentMethodType Method { get; init; }
    public PaymentStatus Status { get; init; }
    public decimal Amount { get; init; }
    public string? TransactionId { get; init; }
    public string? GatewayResponse { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class OrderDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }

    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public PaymentMethodType PaymentMethod { get; init; }

    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public string? CouponCode { get; init; }
    public string? CustomerNote { get; init; }

    public string ShippingName { get; init; } = string.Empty;
    public string ShippingPhone { get; init; } = string.Empty;
    public string ShippingLine1 { get; init; } = string.Empty;
    public string? ShippingLine2 { get; init; }
    public string ShippingCity { get; init; } = string.Empty;
    public string? ShippingState { get; init; }
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string ShippingCountry { get; init; } = string.Empty;

    public DateTime PlacedAt { get; init; }

    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
    public IReadOnlyList<OrderStatusEntryDto> History { get; init; } = Array.Empty<OrderStatusEntryDto>();
    public IReadOnlyList<OrderPaymentDto> Payments { get; init; } = Array.Empty<OrderPaymentDto>();

    public bool AwaitingPayment => PaymentMethod == PaymentMethodType.OnlineGateway
                                   && PaymentStatus is PaymentStatus.Pending or PaymentStatus.Failed;

    /// <summary>Customer can self-cancel before the order enters processing.</summary>
    public bool CanCancel => Status is OrderStatus.Pending or OrderStatus.Confirmed;

    /// <summary>Customer can request a return once the order has been delivered.</summary>
    public bool CanRequestReturn => Status == OrderStatus.Delivered;

    public string ShippingOneLine =>
        string.Join(", ", new[] { ShippingLine1, ShippingLine2, ShippingCity, ShippingState, ShippingPostalCode, ShippingCountry }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}

public sealed class OrderListItemDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime PlacedAt { get; init; }
    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public PaymentMethodType PaymentMethod { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public int ItemCount { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
}

public interface IOrderService
{
    Task<CheckoutContextDto> BuildCheckoutAsync(string userId, string? couponCode, CancellationToken ct = default);
    Task<Result<PlaceOrderResultDto>> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<OrderListItemDto>> ListForCustomerAsync(string userId, CancellationToken ct = default);
    Task<OrderDto?> GetForCustomerAsync(string userId, string orderNumber, CancellationToken ct = default);

    /// <summary>Guest order tracking: matches on order number + email.</summary>
    Task<OrderDto?> TrackAsync(string orderNumber, string email, CancellationToken ct = default);

    /// <summary>Customer self-service cancellation; only allowed while <see cref="OrderDto.CanCancel"/> is true.</summary>
    Task<Result> CancelAsync(string userId, string orderNumber, string? reason, CancellationToken ct = default);

    /// <summary>Customer requests a return; only allowed while <see cref="OrderDto.CanRequestReturn"/> is true. Does not restock — an admin approves by moving the order to Returned.</summary>
    Task<Result> RequestReturnAsync(string userId, string orderNumber, string? reason, CancellationToken ct = default);
}

public sealed class OrderAdminFilter
{
    public string? Search { get; init; }
    public OrderStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public interface IOrderAdminService
{
    Task<PagedResult<OrderListItemDto>> ListAsync(OrderAdminFilter filter, CancellationToken ct = default);
    Task<OrderDto?> GetAsync(int id, CancellationToken ct = default);
    Task<Result> ChangeStatusAsync(int id, OrderStatus status, string? note, string? changedBy, CancellationToken ct = default);
    Task<Result> ChangePaymentStatusAsync(int id, PaymentStatus status, string? changedBy, CancellationToken ct = default);

    Task<OrderMetricsDto> GetMetricsAsync(CancellationToken ct = default);
}

public sealed class OrderMetricsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public decimal RevenuePaid { get; init; }
    public int LowStockProducts { get; init; }
}
