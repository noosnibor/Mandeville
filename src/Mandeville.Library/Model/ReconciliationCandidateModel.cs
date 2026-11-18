namespace Mandeville.Library.Model;

/// <summary>A collection or expense that can be ticked on a reconciliation.</summary>
public class ReconciliationCandidateModel
{
    public string fstrItemType { get; set; } = string.Empty;
    public Guid flngSourceKey { get; set; }
    public DateTime fdtmItemDate { get; set; }
    public string? fstrDescription { get; set; }
    public decimal fcurAmount { get; set; }
    public bool fblnTicked { get; set; }

    /// <summary>Collections add to the balance, expenses take away from it.</summary>
    public decimal SignedAmount => fstrItemType == "Expense" ? -fcurAmount : fcurAmount;
}
