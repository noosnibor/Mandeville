namespace Mandeville.Library.Model;

/// <summary>Finance record (the live version, flngVer = 0).</summary>
public class FinanceModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public int flngMonth { get; set; }
    public int flngYear { get; set; }
    public Guid? flngMemberKey { get; set; }
    public string? fstrMemberName { get; set; }
    public string fstrPaymentMethod { get; set; } = string.Empty;
    public Guid flngPaymentTypeKey { get; set; }
    public decimal fcurAmount { get; set; }
    public string? fstrReceivedBy { get; set; }
    public string? fstrNotes { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrPaymentTypeName { get; set; }
    public bool fblnLocked { get; set; }

    #endregion
}
