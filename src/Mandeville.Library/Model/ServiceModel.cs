namespace Mandeville.Library.Model;

/// <summary>Service record (the live version, flngVer = 0).</summary>
public class ServiceModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string fstrServiceName { get; set; } = string.Empty;
    public DateTime fdtmServiceDate { get; set; }
    public TimeSpan? ftimStartTime { get; set; }
    public string? fstrLocation { get; set; }
    public string? fstrCheckInToken { get; set; }
    public string? fstrPreviousCheckInToken { get; set; }
    public DateTime? fdtmCheckInOpens { get; set; }
    public DateTime? fdtmCheckInCloses { get; set; }
    public bool fblnCheckInOpen { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    /// <summary>True while check-in is switched on and the current time is inside the check-in window.</summary>
    public bool IsOpenNow => fblnCheckInOpen && fdtmCheckInOpens.HasValue && fdtmCheckInCloses.HasValue && AppClock.Now >= fdtmCheckInOpens && AppClock.Now <= fdtmCheckInCloses;
}
