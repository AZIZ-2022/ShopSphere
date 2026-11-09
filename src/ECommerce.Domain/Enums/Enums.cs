namespace ECommerce.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Returned = 6,
    ReturnRequested = 7
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum PaymentMethodType
{
    CashOnDelivery = 0,
    OnlineGateway = 1
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum AddressType
{
    Shipping = 0,
    Billing = 1,
    Both = 2
}
