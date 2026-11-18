using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Mandeville.Web.Security;

/// <summary>Names the security rule of a page ("page:visitors").</summary>
public static class PageAccessPolicy
{
    public const string Prefix = "page:";

    public static string For(string pageKey) => Prefix + pageKey;
}

/// <summary>
/// Makes a rule up on the spot for any "page:xxx" name: the Administrator always passes; everyone else needs the page in the
/// access claims they were given at sign-in (which came from the Roles &amp; access screen). Pages marked AdminOnly never pass for anyone else.
/// </summary>
public sealed class PageAccessPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider __fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => __fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => __fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PageAccessPolicy.Prefix, StringComparison.Ordinal))
            return __fallback.GetPolicyAsync(policyName);

        var key = policyName[PageAccessPolicy.Prefix.Length..];

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context => context.User.CanPage(key))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>Questions pages and the menu ask about the signed-in person.</summary>
public static class AccessExtensions
{
    /// <summary>True when the person may open the page.</summary>
    public static bool CanPage(this ClaimsPrincipal user, string page)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole(AppRoles.Administrator)) return true;
        if (PageRegistry.Find(page)?.AdminOnly == true) return false;
        return user.HasClaim(AppClaims.Page, page);
    }

    /// <summary>True when the person may open this tab of the page (a page without tabs has nothing to restrict).</summary>
    public static bool CanTab(this ClaimsPrincipal user, string page, string tab) =>
        user.IsInRole(AppRoles.Administrator) || (user.CanPage(page) && user.HasClaim(AppClaims.Tab, page + "|" + tab));

    /// <summary>True when the person only sees their own records on the page (never the Administrator).</summary>
    public static bool OwnOnly(this ClaimsPrincipal user, string page) =>
        !user.IsInRole(AppRoles.Administrator) && user.HasClaim(AppClaims.OwnOnly, page);
}
