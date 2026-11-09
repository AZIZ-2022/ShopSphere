using ECommerce.Domain.Enums;

namespace ECommerce.Web.Infrastructure;

public static class OrderUi
{
    public static string StatusClass(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "st-pending",
        OrderStatus.Confirmed => "st-confirmed",
        OrderStatus.Processing => "st-processing",
        OrderStatus.Shipped => "st-shipped",
        OrderStatus.Delivered => "st-delivered",
        OrderStatus.Cancelled => "st-cancelled",
        OrderStatus.Returned => "st-returned",
        OrderStatus.ReturnRequested => "st-return-requested",
        _ => "st-pending"
    };

    public static string PaymentClass(PaymentStatus s) => s switch
    {
        PaymentStatus.Paid => "st-delivered",
        PaymentStatus.Pending => "st-pending",
        PaymentStatus.Failed => "st-cancelled",
        PaymentStatus.Refunded => "st-returned",
        _ => "st-pending"
    };

    public static string PaymentMethodLabel(PaymentMethodType m) => m switch
    {
        PaymentMethodType.CashOnDelivery => "Cash on Delivery",
        PaymentMethodType.OnlineGateway => "Online Payment",
        _ => m.ToString()
    };

    /// <summary>The statuses an admin can move an order to next (plus Cancel).</summary>
    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus current) => current switch
    {
        OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        OrderStatus.Confirmed => new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        OrderStatus.Processing => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        OrderStatus.Shipped => new[] { OrderStatus.Delivered, OrderStatus.Returned },
        OrderStatus.Delivered => new[] { OrderStatus.Returned },
        OrderStatus.ReturnRequested => new[] { OrderStatus.Returned, OrderStatus.Delivered },
        _ => Array.Empty<OrderStatus>()
    };
}
