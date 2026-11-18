using Mandeville.Library.Model;
using Mandeville.Library.Repository;

namespace Mandeville.Web.Security;

/// <summary>
/// First start after the roles tables were added: creates the six roles that already existed, with the same access they had,
/// so nothing changes until the Administrator edits them on the Roles &amp; access screen. Does nothing once any role exists.
/// </summary>
public static class AccessSeeder
{
    public static async Task EnsureAsync(IRoleRepository roleRepo, ILogger logger, CancellationToken cancellationToken = default)
    {
        var existing = await roleRepo.SelectRole(active: null, cancellationToken: cancellationToken);
        if (existing.Count > 0) return;

        // (code, label, locked, pages: key, own-only)
        var seeds = new (string Name, string Label, bool Locked, (string Page, bool Own)[] Pages)[]
        {
            (AppRoles.Administrator, "Administrator", true, []),
            (AppRoles.Minister, "Minister", false, [(PageKeys.Pastoral, false), (PageKeys.Reports, false)]),
            (AppRoles.PastoralCare, "Pastoral Care", false, [(PageKeys.Pastoral, true)]),
            (AppRoles.ConnectionLead, "Connection team lead", false, [(PageKeys.Connect, false), (PageKeys.Incoming, false)]),
            (AppRoles.ConnectionMember, "Connection team member", false, [(PageKeys.Connect, true)]),
            (AppRoles.MemberCare, "Member Care", false, [(PageKeys.Visitors, false)]),
        };

        foreach (var seed in seeds)
        {
            var role = new RoleModel { fstrName = seed.Name, fstrLabel = seed.Label, fblnLocked = seed.Locked, fstrWho = "System" };
            var added = await roleRepo.AddOrUpdateRole(role, insert: true, cancellationToken);
            if (!added.IsSuccess)
            {
                logger.LogError("Could not create the {Role} role: {Message}", seed.Label, added.Message);
                continue;
            }

            if (seed.Pages.Length == 0) continue;

            var permissions = seed.Pages.Select(p => new RolePermissionModel { fstrPage = p.Page, fblnOwnOnly = p.Own });
            var saved = await roleRepo.SaveRolePermissions(role.flngKey, permissions, "System", cancellationToken);
            if (!saved.IsSuccess)
                logger.LogError("Could not save the access of the {Role} role: {Message}", seed.Label, saved.Message);
        }

        logger.LogInformation("The starting roles were created.");
    }
}
