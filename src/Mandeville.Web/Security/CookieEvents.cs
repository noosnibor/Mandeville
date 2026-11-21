using System.Security.Claims;
using Mandeville.Library.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Mandeville.Web.Security;

/// <summary>
/// Re-checks a sign-in cookie against the account, so a deactivated or changed account stops working.
/// It also keeps the roles and access claims up to date: when the Administrator changes a person's roles, or what a role may open,
/// the cookie is refreshed within about a minute (no sign-out needed; open pages show the change when they are next loaded).
/// </summary>
public static class CookieEvents
{
    private sealed record Status(bool Active, List<string> Roles, Guid? MemberKey);

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
            status = user is null ? new Status(false, [], null) : new Status(user.fblnActive, user.GetAllRoles(), user.flngMemberKey);
            cache.Set(cacheKey, status, __recheckEvery);
        }

        var cookieMember = Guid.TryParse(principal.FindFirstValue(AppClaims.MemberKey), out var m) ? m : (Guid?)null;
        if (status is null || !status.Active || status.MemberKey != cookieMember)
        {
            await Reject(context);
            return;
        }

        await RefreshAccessAsync(context, cache, principal, status.Roles);
    }

    /// <summary>Swaps the cookie's roles, label and access claims for the person's current ones when they differ.</summary>
    private static async Task RefreshAccessAsync(CookieValidatePrincipalContext context, IMemoryCache cache, ClaimsPrincipal principal, List<string> roles)
    {
        var cacheKey = "auth-access-" + string.Join('|', roles.Order(StringComparer.Ordinal));

        if (!cache.TryGetValue(cacheKey, out List<Claim>? fresh) || fresh is null)
        {
            var roleRepo = context.HttpContext.RequestServices.GetRequiredService<IRoleRepository>();
            fresh = await RoleAccess.BuildClaimsAsync(roleRepo, roles);
            cache.Set(cacheKey, fresh, __recheckEvery);
        }

        if (RoleAccess.SameAsCookie(principal, roles, fresh)) return;

        var kept = principal.Claims.Where(c => c.Type != ClaimTypes.Role && c.Type != AppClaims.RoleLabel && !AccessClaims.IsAccessClaim(c));
        context.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(kept.Concat(fresh).ToList(), "Cookies", ClaimTypes.Name, ClaimTypes.Role)));
        context.ShouldRenew = true;
    }

    private static async Task Reject(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
