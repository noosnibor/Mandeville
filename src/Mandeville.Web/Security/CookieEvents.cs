using System.Security.Claims;
using Mandeville.Library.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Mandeville.Web.Security;

/// <summary>
/// Re-checks a sign-in cookie against the account, so a deactivated or changed account stops working.
/// It also keeps the access claims up to date: when the Administrator changes a role on the Roles &amp; access screen,
/// the cookie is refreshed within about a minute (no sign-out needed; open pages show the change when they are next loaded).
/// </summary>
public static class CookieEvents
{
    private sealed record Status(bool Active, string Role, Guid? MemberKey);

    private static readonly TimeSpan __recheckEvery = TimeSpan.FromSeconds(60);

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userKey))
        {
            await Reject(context);
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        var cacheKey = "auth-status-" + userKey;

        if (!cache.TryGetValue(cacheKey, out Status? status))
        {
            var repo = context.HttpContext.RequestServices.GetRequiredService<IAppUserRepository>();
            var user = (await repo.SelectAppUser(key: userKey)).FirstOrDefault();
            status = user is null ? new Status(false, string.Empty, null) : new Status(user.fblnActive, user.fstrRole, user.flngMemberKey);
            cache.Set(cacheKey, status, __recheckEvery);
        }

        var cookieMember = Guid.TryParse(principal.FindFirstValue(AppClaims.MemberKey), out var m) ? m : (Guid?)null;
        if (status is null || !status.Active || status.Role != principal.FindFirstValue(ClaimTypes.Role) || status.MemberKey != cookieMember)
        {
            await Reject(context);
            return;
        }

        // The Administrator is allowed everything by rule, so there are no access claims to keep fresh
        if (status.Role == AppRoles.Administrator) return;

        await RefreshAccessAsync(context, cache, principal, status.Role);
    }

    /// <summary>Swaps the cookie's access claims for the role's current ones when they differ.</summary>
    private static async Task RefreshAccessAsync(CookieValidatePrincipalContext context, IMemoryCache cache, ClaimsPrincipal principal, string role)
    {
        var cacheKey = "auth-access-" + role;

        if (!cache.TryGetValue(cacheKey, out List<Claim>? fresh) || fresh is null)
        {
            var roleRepo = context.HttpContext.RequestServices.GetRequiredService<IRoleRepository>();
            var permissions = await roleRepo.SelectRolePermission(roleName: role);
            fresh = AccessClaims.Build(permissions);
            cache.Set(cacheKey, fresh, __recheckEvery);
        }

        var freshSignature = fresh.FirstOrDefault(c => c.Type == AccessClaims.SignatureType)?.Value;
        if (principal.FindFirstValue(AccessClaims.SignatureType) == freshSignature) return;

        var claims = principal.Claims.Where(c => !AccessClaims.IsAccessClaim(c)).Concat(fresh).ToList();
        context.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", ClaimTypes.Name, ClaimTypes.Role)));
        context.ShouldRenew = true;
    }

    private static async Task Reject(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
