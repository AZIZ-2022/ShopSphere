using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

/// <summary>Application role. Wraps <see cref="IdentityRole"/> to allow a description later.</summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public string? Description { get; set; }
}

/// <summary>Well-known role names used across the application.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static readonly string[] All = { Admin, Customer };
}
