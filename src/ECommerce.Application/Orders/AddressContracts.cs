using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders;

public sealed class AddressDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? State { get; init; }
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public AddressType AddressType { get; init; }
    public bool IsDefault { get; init; }

    public string OneLine =>
        string.Join(", ", new[] { Line1, Line2, City, State, PostalCode, Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}

public sealed class AddressEditModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(250)]
    [Display(Name = "Address line 1")]
    public string Line1 { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Address line 2")]
    public string? Line2 { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "State / Division")]
    public string? State { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Postal code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "Bangladesh";

    [Display(Name = "Set as default")]
    public bool IsDefault { get; set; }
}

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> ListAsync(string userId, CancellationToken ct = default);
    Task<AddressDto?> GetAsync(string userId, int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(string userId, AddressEditModel model, CancellationToken ct = default);
    Task<Result> UpdateAsync(string userId, AddressEditModel model, CancellationToken ct = default);
    Task<Result> DeleteAsync(string userId, int id, CancellationToken ct = default);
    Task<Result> SetDefaultAsync(string userId, int id, CancellationToken ct = default);
}
