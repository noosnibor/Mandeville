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

    public bool IsLocked => fdtmLockedUntil is not null && fdtmLockedUntil > AppClock.Now;
}
