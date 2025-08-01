using System.Security.Claims;
using CTOM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CTOM.Services.Identity;

/// <summary>
/// Custom claims principal factory that automatically injects domain-specific claims
/// (e.g. <c>MaPhong</c>, <c>TenUser</c>) into the <see cref="ClaimsPrincipal" />
/// every time a user signs in. This ensures <see cref="ICurrentUserAccessor" />
/// can reliably read these values without additional DB calls.
/// </summary>
public sealed class AppClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrEmpty(user.MaPhong))
        {
            identity.AddClaim(new Claim("MaPhong", user.MaPhong));
        }

        // Optional but handy for UI display / logging
        if (!string.IsNullOrEmpty(user.TenUser))
        {
            identity.AddClaim(new Claim("TenUser", user.TenUser));
        }

        return identity;
    }
}
