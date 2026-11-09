using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Payments;

namespace ECommerce.Web.Models.Orders;

public sealed class PaymentPageModel
{
    public required PayableOrderDto Order { get; init; }
    public CardForm Card { get; set; } = new();

    /// <summary>Set when a hosted/redirect gateway (e.g. SSLCommerz) failed to start a session.</summary>
    public string? GatewayError { get; init; }
}

public sealed class CardForm
{
    [Required]
    [Display(Name = "Name on card")]
    public string CardHolder { get; set; } = string.Empty;

    [Required]
    [CreditCard(ErrorMessage = "Enter a valid card number.")]
    [Display(Name = "Card number")]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Use MM/YY.")]
    [Display(Name = "Expiry (MM/YY)")]
    public string Expiry { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "3–4 digits.")]
    public string Cvc { get; set; } = string.Empty;
}
