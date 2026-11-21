using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Mandeville.Library.Model;

namespace Mandeville.Web.Security;

/// <summary>Turns a role's saved pages into the claims stored (signed and encrypted) in the sign-in cookie.</summary>
public static class AccessClaims
{
    /// <summary>A fingerprint of the access claims, used to notice when the role has been changed since sign-in.</summary>
    public const string SignatureType = "access_sig";

    /// <summary>The access claims for one or more roles (pass every role's permissions together), ending with the fingerprint claim.</summary>
    public static List<Claim> Build(IEnumerable<RolePermissionModel> permissions)
    {
        var claims = new List<Claim>();

        // A person may have several roles, so the same page can appear more than once: their access is the combination
        foreach (var group in permissions.GroupBy(p => p.fstrPage, StringComparer.OrdinalIgnoreCase))
        {
            var page = PageRegistry.Find(group.Key);
            if (page is null || page.AdminOnly) continue; // a page that no longer exists, or one that can never be given away

            claims.Add(new Claim(AppClaims.Page, page.Key));

            // "Own records only" holds only when every role that opens the page is limited that way
            if (page.OwnOnlyOption && group.All(p => p.fblnOwnOnly))
                claims.Add(new Claim(AppClaims.OwnOnly, page.Key));

            // No saved tab list means every tab (including any added later); otherwise the tabs of all the roles are added together
            var saved = group.Select(p => PageRegistry.SplitTabs(p.fstrTabs)).ToList();
            var tabs = saved.Any(list => list.Length == 0)
                ? page.Tabs
                : page.Tabs.Where(t => saved.Any(list => list.Contains(t, StringComparer.OrdinalIgnoreCase)));
            foreach (var tab in tabs)
                claims.Add(new Claim(AppClaims.Tab, page.Key + "|" + tab));
        }

        claims.Add(new Claim(SignatureType, Signature(claims)));
        return claims;
    }

    /// <summary>True for the claims that come from the Roles &amp; access screen.</summary>
    public static bool IsAccessClaim(Claim claim) =>
        claim.Type is AppClaims.Page or AppClaims.Tab or AppClaims.OwnOnly or SignatureType;

    private static string Signature(IEnumerable<Claim> claims)
    {
        var text = string.Join(';', claims.Select(c => c.Type + "=" + c.Value).OrderBy(x => x, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
    }
}
