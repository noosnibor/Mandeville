namespace Mandeville.Library.Model;

/// <summary>Member Attendance record (the live version, flngVer = 0).</summary>
public class MemberAttendanceModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngServiceKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public DateTime fdtmCheckedInAt { get; set; }
    public string fstrMethod { get; set; } = string.Empty;
    public string? fstrNotes { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrMemberFirstname { get; set; }
    public string? fstrMemberLastname { get; set; }

    #endregion

    public string MemberName => $"{fstrMemberFirstname} {fstrMemberLastname}";
}
