namespace Mandeville.Library.Model;

/// <summary>A person who can sign in to the system (live version, flngVer = 0).</summary>
public class AppUserModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string fstrEmail { get; set; } = string.Empty;
    public string fstrDisplayName { get; set; } = string.Empty;
    public string fstrPasswordHash { get; set; } = string.Empty;
    public string fstrRole { get; set; } = "PastoralCare";

    /// <summary>More role codes (a comma list) on top of the primary role. Access is the combination of all roles.</summary>
    public string? fstrExtraRoles { get; set; }

    /// <summary>The Core Member this account belongs to (Pastoral Care accounts).</summary>
    public Guid? flngMemberKey { get; set; }
    public bool fblnMustChangePassword { get; set; } = true;
    public int flngFailedAttempts { get; set; }
    public DateTime? fdtmLockedUntil { get; set; }
    public DateTime? fdtmLastSignIn { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined)

    public string? fstrMemberName { get; set; }

    #endregion

    #region Roles

    /// <summary>The role codes in a comma list, trimmed, without blanks or repeats.</summary>
    public static List<string> SplitRoles(string? list) =>
        (list ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>The primary role first, then the extra roles (no repeats). The Administrator is never an extra role.</summary>
    public List<string> GetAllRoles()
    {
        var roles = new List<string>();
        if (!string.IsNullOrWhiteSpace(fstrRole)) roles.Add(fstrRole);
        roles.AddRange(SplitRoles(fstrExtraRoles).Where(r => r != "Administrator" && !roles.Contains(r, StringComparer.OrdinalIgnoreCase)));
        return roles;
    }

    #endregion

    public bool IsLocked => fdtmLockedUntil is not null && fdtmLockedUntil > AppClock.Now;
}
