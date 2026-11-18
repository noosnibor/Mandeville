namespace Mandeville.Library.Model;

/// <summary>A stored ledger period row (flngVer = 0). Only periods that were closed, or that carry the opening balance, have a row.</summary>
public class LedgerPeriodModel
{
    public Guid flngKey { get; set; }
    public int flngYear { get; set; }
    public int flngMonth { get; set; }
    public decimal? fcurOpeningBalance { get; set; }
    public string fstrStatus { get; set; } = "Open";
    public decimal? fcurBroughtForward { get; set; }
    public decimal? fcurCollections { get; set; }
    public decimal? fcurExpenses { get; set; }
    public decimal? fcurClosingBalance { get; set; }
    public string? fstrClosedBy { get; set; }
    public DateTime? fdtmClosedOn { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }
}

/// <summary>Collections and expenses totalled for one calendar month.</summary>
public class LedgerTotalModel
{
    public int flngYear { get; set; }
    public int flngMonth { get; set; }
    public decimal fcurCollections { get; set; }
    public int flngCollectionRecords { get; set; }
    public decimal fcurExpenses { get; set; }
    public int flngExpenseRecords { get; set; }
}

/// <summary>One month of the financial ledger as shown on the page (calculated, not stored).</summary>
public class LedgerMonthModel
{
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Balance carried in from the previous month (or the opening balance for the first month).</summary>
    public decimal BroughtForward { get; set; }
    public decimal Collections { get; set; }
    public decimal Expenses { get; set; }
    public int CollectionRecords { get; set; }
    public int ExpenseRecords { get; set; }

    /// <summary>Balance carried forward: brought forward + collections - expenses.</summary>
    public decimal Balance => BroughtForward + Collections - Expenses;

    public bool IsClosed { get; set; }
    public bool IsCurrent { get; set; }

    /// <summary>True for months after today with no records, and for months before the ledger starts.</summary>
    public bool NotStarted { get; set; }

    /// <summary>True when the month is the first month of the ledger (it carries the opening balance).</summary>
    public bool IsStart { get; set; }

    /// <summary>True when every earlier month is closed, so this month may be closed now.</summary>
    public bool CanClose { get; set; }

    public string? ClosedBy { get; set; }
    public DateTime? ClosedOn { get; set; }
    public int Records => CollectionRecords + ExpenseRecords;
}
