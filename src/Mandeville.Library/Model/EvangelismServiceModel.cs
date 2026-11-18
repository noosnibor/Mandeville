namespace Mandeville.Library.Model;

/// <summary>Evangelism Service record (the live version, flngVer = 0).</summary>
public class EvangelismServiceModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public DateTime fdtmServiceDate { get; set; }
    public TimeSpan? ftimStartTime { get; set; }
    public string? fstrLocation { get; set; }
    public int flngTotalPresent { get; set; }
    public int flngTotalCallers { get; set; }
    public int flngTotalPrayers { get; set; }
    public int flngTotalMinistered { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion
}
