namespace Mandeville.Library.Model;

/// <summary>A WAFIF branch that visiting members come from (managed in Setup, Branches). The live version is flngVer = 0.</summary>
public class BranchModel
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
