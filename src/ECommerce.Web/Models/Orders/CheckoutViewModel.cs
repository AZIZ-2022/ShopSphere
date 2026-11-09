using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Orders;
using ECommerce.Domain.Enums;

namespace ECommerce.Web.Models.Orders;

public sealed class CheckoutViewModel
{
    public required CheckoutContextDto Context { get; init; }
    public CheckoutForm Form { get; set; } = new();
    public string? CouponMessage { get; set; }
    public bool CouponApplied { get; set; }
}

public sealed class CheckoutForm
{
    /// <summary>Chosen saved address; 0/null means "use the new address below".</summary>
    public int? ShippingAddressId { get; set; }

    public AddressEditModel NewAddress { get; set; } = new();

    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.CashOnDelivery;

    [StringLength(40)]
    public string? CouponCode { get; set; }

    [StringLength(1000)]
    public string? CustomerNote { get; set; }
}
