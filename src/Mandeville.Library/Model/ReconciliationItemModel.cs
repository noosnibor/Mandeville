namespace Mandeville.Library.Model;

/// <summary>Reconciliation Item record (the live version, flngVer = 0).</summary>
public class ReconciliationItemModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngReconciliationKey { get; set; }
    public string fstrItemType { get; set; } = string.Empty;
    public Guid flngSourceKey { get; set; }
    public decimal fcurAmount { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion
}
