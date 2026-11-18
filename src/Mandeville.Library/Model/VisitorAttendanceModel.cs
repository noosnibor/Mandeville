namespace Mandeville.Library.Model;

/// <summary>Visitor Attendance record (the live version, flngVer = 0).</summary>
public class VisitorAttendanceModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngVisitorKey { get; set; }
    public DateTime fdtmServiceDate { get; set; }
    public string? fstrAltarReason { get; set; }
    public string? fstrNotes { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrVisitorFirstname { get; set; }
    public string? fstrVisitorLastname { get; set; }
    public string? fstrVisitorGender { get; set; }
    public string? fstrVisitorAgeGroup { get; set; }

    #endregion

    public string VisitorName => $"{fstrVisitorFirstname} {fstrVisitorLastname}";
}
