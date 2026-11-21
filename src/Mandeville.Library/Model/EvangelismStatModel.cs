namespace Mandeville.Library.Model;

/// <summary>The numbers for one scheduled session (no names). Feeds the statistics tab and the service report.</summary>
public class EvangelismStatModel
{
    public Guid flngSessionKey { get; set; }
    public DateTime fdtmServiceDate { get; set; }
    public string fstrArea { get; set; } = string.Empty;
    public string? fstrLocation { get; set; }
    public int flngSlots { get; set; }
    public int flngScheduled { get; set; }
    public int flngPresent { get; set; }
    public int flngFlyers { get; set; }
    public int flngMinistered { get; set; }
    public bool fblnResultsDone { get; set; }
    public int flngNewContacts { get; set; }
    public int flngCalls { get; set; }
    public int flngReached { get; set; }
    /// <summary>Team members who logged at least one call for this session.</summary>
    public int flngCallers { get; set; }

    /// <summary>Share of the scheduled people who turned up, 0 to 100.</summary>
    public int TurnUpPercent => flngScheduled == 0 ? 0 : Math.Min(100, (int)Math.Round(100.0 * flngPresent / flngScheduled));
}
