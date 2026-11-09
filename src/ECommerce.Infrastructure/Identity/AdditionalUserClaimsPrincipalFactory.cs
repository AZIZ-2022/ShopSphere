using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Identity;

/// <summary>
/// Adds a <c>full_name</c> claim so the UI can greet the user by name without a DB round-trip.
/// </summary>
public sealed class AdditionalUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public const string FullNameClaimType = "full_name";

    public AdditionalUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        if (!string.IsNullOrWhiteSpace(user.FullName))
            identity.AddClaim(new Claim(FullNameClaimType, user.FullName));

        if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl))
            identity.AddClaim(new Claim("avatar_url", user.ProfileImageUrl));

        return principal;
    }
}
