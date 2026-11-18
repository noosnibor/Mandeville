namespace Mandeville.Web.Security;

/// <summary>
/// The codes of the roles that come with the system. Roles are now managed on the Roles &amp; access screen,
/// so only the Administrator is relied on by code; the others are the starting roles created on first run.
/// </summary>
public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Minister = "Minister";
    public const string PastoralCare = "PastoralCare";
    public const string ConnectionLead = "ConnectionLead";
    public const string ConnectionMember = "ConnectionMember";
    public const string MemberCare = "MemberCare";


    /// <summary>Friendly text for the starting roles (the screen uses the saved label; this is only a fallback).</summary>
    public static string Label(string? role) => role switch
    {
        Administrator => "Administrator",
        Minister => "Minister",
        PastoralCare => "Pastoral Care",
        ConnectionLead => "Connection team lead",
        ConnectionMember => "Connection team member",
        MemberCare => "Member Care",
        _ => role ?? string.Empty
    };
}

/// <summary>Custom claim types stored in the sign-in cookie.</summary>
public static class AppClaims
{
    /// <summary>The Core Member the account belongs to (Pastoral Care accounts).</summary>
    public const string MemberKey = "member_key";

    /// <summary>Present (and "1") while the person must choose a new password.</summary>
    public const string MustChange = "must_change";

    /// <summary>The friendly name of the person's role.</summary>
    public const string RoleLabel = "role_label";

    /// <summary>A page the person's role may open (one claim per page).</summary>
    public const string Page = "page";

    /// <summary>A tab the person's role may open, written "page|Tab name".</summary>
    public const string Tab = "tab";

    /// <summary>A page where the person only sees their own records.</summary>
    public const string OwnOnly = "own_only";
}
