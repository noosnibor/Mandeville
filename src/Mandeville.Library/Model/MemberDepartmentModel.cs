namespace Mandeville.Library.Model;

/// <summary>Member Department record (the live version, flngVer = 0).</summary>
public class MemberDepartmentModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public Guid flngDepartmentKey { get; set; }
    public bool fblnLead { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrDepartmentName { get; set; }

    #endregion
}
