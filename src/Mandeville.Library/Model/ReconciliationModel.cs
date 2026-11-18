namespace Mandeville.Library.Model;

/// <summary>Reconciliation record (the live version, flngVer = 0).</summary>
public class ReconciliationModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public int flngMonth { get; set; }
    public int flngYear { get; set; }
    public decimal fcurOpeningBalance { get; set; }
    public decimal fcurStatementBalance { get; set; }
    public decimal fcurExpectedBalance { get; set; }
    public string fstrStatus { get; set; } = string.Empty;
    public string? fstrVarianceNote { get; set; }
    public string? fstrReconciledBy { get; set; }
    public DateTime? fdtmReconciledOn { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion
}
