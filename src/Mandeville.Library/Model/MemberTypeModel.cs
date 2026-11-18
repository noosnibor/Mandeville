namespace Mandeville.Library.Model;

/// <summary>Member Type record (the live version, flngVer = 0).</summary>
public class MemberTypeModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string fstrName { get; set; } = string.Empty;
    public string? fstrDescription { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion
}
