namespace Mandeville.Library.Model;

/// <summary>A role people can be given (live version, flngVer = 0). Which pages it opens is kept in <see cref="RolePermissionModel"/>.</summary>
public class RoleModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public int flngVer { get; set; }

    /// <summary>The code stored on each account. It never changes, so renaming a role does not touch the accounts.</summary>
    public string fstrName { get; set; } = string.Empty;

    /// <summary>The friendly name shown on screen.</summary>
    public string fstrLabel { get; set; } = string.Empty;

    /// <summary>The Administrator role: always full access, cannot be edited or removed.</summary>
    public bool fblnLocked { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined)

    /// <summary>How many active accounts have this role.</summary>
    public int flngUsers { get; set; }

    /// <summary>Comma list of the pages where this role only sees its own records (empty when none).</summary>
    public string? fstrOwnPages { get; set; }

    #endregion

    /// <summary>True when accounts with this role belong to one member (they only see their own records somewhere).</summary>
    public bool NeedsMember => !string.IsNullOrWhiteSpace(fstrOwnPages);

    /// <summary>True when the role only sees its own records on the given page.</summary>
    public bool OwnsPage(string page) =>
        !string.IsNullOrWhiteSpace(fstrOwnPages) && fstrOwnPages.Split(',').Contains(page, StringComparer.OrdinalIgnoreCase);
}

/// <summary>One page (and optionally some of its tabs) that a role may open.</summary>
public class RolePermissionModel
{
    public Guid flngKey { get; set; }
    public Guid flngRoleKey { get; set; }

    /// <summary>The page key declared on the page itself.</summary>
    public string fstrPage { get; set; } = string.Empty;

    /// <summary>Comma list of the allowed tabs; empty or null means every tab.</summary>
    public string? fstrTabs { get; set; }

    /// <summary>The role only sees its own records on this page.</summary>
    public bool fblnOwnOnly { get; set; }
}
