using System.Security.Claims;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;

namespace Mandeville.Web.Security;

/// <summary>
/// Builds the role part of the sign-in cookie for a person who may have more than one role:
/// one role claim each, one combined label, and the combined page access.
/// Used at sign-in and again whenever the cookie is refreshed, so both always agree.
/// </summary>
public static class RoleAccess
{
    /// <summary>The joiner between role names in the combined label.</summary>
    private const string LabelSeparator = " + ";

    /// <summary>The role claims, the role label claim and the access claims for the given roles (primary role first).</summary>
    public static async Task<List<Claim>> BuildClaimsAsync(IRoleRepository roleRepo, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();
        var labels = new List<string>();
        var permissions = new List<RolePermissionModel>();

        foreach (var code in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, code));

            var role = (await roleRepo.SelectRole(name: code, cancellationToken: cancellationToken)).FirstOrDefault();
            labels.Add(role?.fstrLabel ?? AppRoles.Label(code));

            // The Administrator is allowed everything by rule, so it has no page list of its own
            if (code != AppRoles.Administrator)
                permissions.AddRange(await roleRepo.SelectRolePermission(roleName: code, cancellationToken: cancellationToken));
        }

        claims.Add(new Claim(AppClaims.RoleLabel, string.Join(LabelSeparator, labels)));

        // An Administrator carries no page claims at all
        if (!roles.Contains(AppRoles.Administrator))
            claims.AddRange(AccessClaims.Build(permissions));

        return claims;
    }

    /// <summary>True when the claims are the same as the person's current cookie (same roles, label and access).</summary>
    public static bool SameAsCookie(ClaimsPrincipal principal, IReadOnlyList<string> roles, IReadOnlyList<Claim> fresh)
    {
        var cookieRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        if (!cookieRoles.SetEquals(roles)) return false;

        var freshLabel = fresh.FirstOrDefault(c => c.Type == AppClaims.RoleLabel)?.Value;
        if (principal.FindFirstValue(AppClaims.RoleLabel) != freshLabel) return false;

        var freshSignature = fresh.FirstOrDefault(c => c.Type == AccessClaims.SignatureType)?.Value;
        return principal.FindFirstValue(AccessClaims.SignatureType) == freshSignature;
    }
}
