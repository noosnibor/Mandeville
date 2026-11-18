namespace Mandeville.Library.Model;

/// <summary>Expenditure record (the live version, flngVer = 0).</summary>
public class ExpenditureModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public DateTime fdtmExpenseDate { get; set; }
    public Guid flngExpenseCategoryKey { get; set; }
    public string fstrPayee { get; set; } = string.Empty;
    public string? fstrDescription { get; set; }
    public string fstrPaymentMethod { get; set; } = string.Empty;
    public string? fstrReferenceNumber { get; set; }
    public decimal fcurAmount { get; set; }
    public string? fstrPaidBy { get; set; }
    public string? fstrNotes { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrExpenseCategoryName { get; set; }
    public bool fblnLocked { get; set; }

    #endregion
}
