using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Orders;

namespace ECommerce.Web.Models.Orders;

public sealed class TrackOrderModel
{
    [Required]
    [Display(Name = "Order number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email used on the order")]
    public string Email { get; set; } = string.Empty;

    public OrderDto? Result { get; set; }
    public bool Searched { get; set; }
}
