namespace Mandeville.Library.Model;

/// <summary>Evangelism Assignment record (the live version, flngVer = 0).</summary>
public class EvangelismAssignmentModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngServiceKey { get; set; }
    public Guid? flngMemberKey { get; set; }
    public string? fstrTitle { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string? fstrEmailAddress { get; set; }
    public string? fstrSchedule { get; set; }
    public string? fstrAssignment { get; set; }
    public string fstrRole { get; set; } = string.Empty;
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    /// <summary>Scheduled, Confirmed, Declined, Attended or Absent.</summary>
    public string fstrStatus { get; set; } = EvangelismRules.Scheduled;

    #endregion

    #region Read-only (joined from the session)

    public DateTime fdtmSessionDate { get; set; }
    public TimeSpan? ftimSessionTime { get; set; }
    public string? fstrSessionArea { get; set; }
    public string? fstrSessionLocation { get; set; }

    #endregion

    public string DisplayName => string.IsNullOrWhiteSpace(fstrTitle) ? $"{fstrFirstname} {fstrLastname}" : $"{fstrTitle.Trim()} {fstrFirstname} {fstrLastname}";

    /// <summary>A copy to edit without touching the row on screen.</summary>
    public EvangelismAssignmentModel Copy() => (EvangelismAssignmentModel)MemberwiseClone();
}
