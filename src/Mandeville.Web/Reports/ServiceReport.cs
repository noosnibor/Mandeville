namespace Mandeville.Web.Reports;

/// <summary>Everything printed on one service report. Numbers only: no names and no care notes.</summary>
public class ServiceReport
{
    #region Service

    public Guid ServiceKey { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public DateTime GeneratedOn { get; set; }

    #endregion

    #region Visitors

    public int VisitorsAttended { get; set; }
    public int VisitorsMale { get; set; }
    public int VisitorsFemale { get; set; }
    public int VisitorsAdult { get; set; }
    public int VisitorsChild { get; set; }
    public int WentToAltar { get; set; }

    /// <summary>Total for each reason for going to the altar (every known reason is listed, even with 0).</summary>
    public List<(string Reason, int Count)> AltarReasons { get; set; } = [];

    public int Baptised { get; set; }
    public int Called { get; set; }
    public int StartedClass { get; set; }
    public int WaitingForConnector { get; set; }

    #endregion

    #region Incoming members

    /// <summary>Who is in the membership process right now (people already converted drop off).</summary>
    public int IncomingTotal { get; set; }
    public int IncomingNewBelievers { get; set; }
    public int IncomingReturning { get; set; }
    public int IncomingJoining { get; set; }
    public int IncomingPartners { get; set; }

    /// <summary>Stages completed so far. Partners are left out.</summary>
    public int Tier1Done { get; set; }
    public int Tier2Done { get; set; }
    public int InterviewDone { get; set; }
    public int InauguratedDone { get; set; }

    /// <summary>Progress from the service date until the next week.</summary>
    public int WeekTier1 { get; set; }
    public int WeekTier2 { get; set; }
    public int WeekInterview { get; set; }
    public int WeekInaugurated { get; set; }
    public int WeekBaptised { get; set; }

    public int ReadyToConvert { get; set; }
    public int IncomingWaitingForConnector { get; set; }

    #endregion

    #region Members and pastoral care

    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int InactiveMembers { get; set; }
    public int MembersPresent { get; set; }

    /// <summary>Members from other branches who attended (not part of the member list or the percentage).</summary>
    public int VisitingMembers { get; set; }
    public int TotalInService => MembersPresent + VisitingMembers;
    public List<KeyValuePair<string, int>> VisitingByBranch { get; set; } = [];
    public int PresentPercent { get; set; }
    public int Connected { get; set; }
    public int Careable { get; set; }
    public int CoreMembers { get; set; }
    public int CareContactsThisWeek { get; set; }
    public int NotSeen3Weeks { get; set; }
    public int OverdueForContact { get; set; }
    public int FlaggedForMinister { get; set; }

    /// <summary>Active members grouped by title (numbers only). Members without a title are counted under "No title".</summary>
    public List<(string Title, int Count)> MembersByTitle { get; set; } = [];

    #endregion

    #region Evangelism (numbers only, never names)

    /// <summary>True when any outreach session was planned in the month to date, so empty sections can be left out.</summary>
    public bool HasEvangelism => EvangelismSaturday.Sessions > 0 || EvangelismMonth.Sessions > 0;

    /// <summary>The week up to and including the service date.</summary>
    public EvangelismNumbers EvangelismSaturday { get; set; } = new();

    /// <summary>The first of the month up to the service date.</summary>
    public EvangelismNumbers EvangelismMonth { get; set; } = new();

    /// <summary>People met on the street who are still being followed up.</summary>
    public int ContactsActive { get; set; }
    public int ContactsConnected { get; set; }
    public int ContactsUnreachable { get; set; }

    #endregion

    #region Finance

    public string FinanceMonth { get; set; } = string.Empty;
    public decimal DuesCollected { get; set; }
    public decimal OtherCollected { get; set; }
    public decimal TotalCollected => DuesCollected + OtherCollected;
    public DateTime ExpensesFrom { get; set; }
    public decimal Expenses { get; set; }
    public List<(string Category, decimal Amount)> TopExpenses { get; set; } = [];

    #endregion

    public string FileName => $"Service Report {ServiceDate:yyyy-MM-dd}.pdf";
}

/// <summary>Totals for a stretch of outreach sessions.</summary>
public class EvangelismNumbers
{
    public int Sessions { get; set; }
    public int Scheduled { get; set; }
    public int Present { get; set; }
    public int Flyers { get; set; }
    public int Ministered { get; set; }
    public int NewContacts { get; set; }
    public int Calls { get; set; }
    public int Reached { get; set; }

    public int TurnUpPercent => Scheduled == 0 ? 0 : Math.Min(100, (int)Math.Round(100.0 * Present / Scheduled));
}
