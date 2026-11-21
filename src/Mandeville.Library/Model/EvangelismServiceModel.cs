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

    /// <summary>Street Evangelism, Tele Evangelism or Intercessory Prayer.</summary>
    public string fstrArea { get; set; } = EvangelismRules.Street;
    /// <summary>The member who leads this group and enters its numbers.</summary>
    public Guid? flngLeadMemberKey { get; set; }
    /// <summary>How many people are needed (the team lead included).</summary>
    public int flngSlots { get; set; }
    public int flngFlyers { get; set; }
    public string? fstrNotes { get; set; }
    public bool fblnResultsDone { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrLeadName { get; set; }
    /// <summary>People scheduled (anyone who has not declined).</summary>
    public int flngAssigned { get; set; }

    #endregion

    /// <summary>A copy to edit without touching the row on screen.</summary>
    public EvangelismServiceModel Copy() => (EvangelismServiceModel)MemberwiseClone();
}
