namespace Mandeville.Library.Model;

/// <summary>One value in an editable fixed list (marital status, payment method and so on).</summary>
public class LookupValueModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string fstrList { get; set; } = string.Empty;
    public string fstrValue { get; set; } = string.Empty;
    public int flngSort { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion
}
