namespace Mandeville.Library.Model;

/// <summary>Visitor record (the live version, flngVer = 0).</summary>
public class VisitorModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string fstrGender { get; set; } = string.Empty;
    public string fstrAgeGroup { get; set; } = string.Empty;
    public string? fstrPhoneNumber { get; set; }
    public string? fstrEmailAddress { get; set; }
    public string? fstrAddress { get; set; }
    public DateTime? fdtmLastContactDate { get; set; }
    public DateTime? fdtmNextContactDate { get; set; }
    public Guid? flngFollowUpMemberKey { get; set; }
    public string fstrStatus { get; set; } = string.Empty;
    public string? fstrJoinedChurch { get; set; }
    public string? fstrMembershipTransferNote { get; set; }
    public DateTime? fdtmBaptisedOn { get; set; }
    /// <summary>Membership process: Tier 1 (the class start date), Tier 2, Interview and Inauguration.</summary>
    public DateTime? fdtmClassStartedOn { get; set; }
    public DateTime? fdtmTier2On { get; set; }
    public DateTime? fdtmInterviewOn { get; set; }
    public DateTime? fdtmInauguratedOn { get; set; }
    /// <summary>Date of birth, added by the connection team member who follows the person up.</summary>
    public DateTime? fdtmDateOfBirth { get; set; }
    /// <summary>Incoming member path: New believer, Returning, Joining the church or Partner.</summary>
    public string? fstrPath { get; set; }
    public Guid? flngMemberKey { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrFollowUpName { get; set; }
    public DateTime? fdtmLastAttended { get; set; }
    public int flngVisitCount { get; set; }

    #endregion

    public string FullName => $"{fstrFirstname} {fstrLastname}";
}
