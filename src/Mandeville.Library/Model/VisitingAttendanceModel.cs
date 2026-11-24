namespace Mandeville.Library.Model;

/// <summary>A WAFIF member from another branch who was present at a service (the live version, flngVer = 0).</summary>
public class VisitingAttendanceModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngServiceKey { get; set; }
    public Guid flngVisitingMemberKey { get; set; }
    public DateTime fdtmCheckedInAt { get; set; }
    public string fstrMethod { get; set; } = string.Empty;
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined)

    public string? fstrTitle { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string fstrBranch { get; set; } = string.Empty;

    #endregion

    /// <summary>"Deacon Brian Clarke" (the title is left out when there is none).</summary>
    public string FullName => string.Join(' ', new[] { fstrTitle, fstrFirstname, fstrLastname }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
