using System.ComponentModel.DataAnnotations;

namespace ECommerce.Web.Models.Home;

public sealed class ContactFormModel
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Subject { get; set; }

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}
