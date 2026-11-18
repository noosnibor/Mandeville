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
    public int PresentPercent { get; set; }
    public int Connected { get; set; }
    public int Careable { get; set; }
    public int CoreMembers { get; set; }
    public int CareContactsThisWeek { get; set; }
    public int NotSeen3Weeks { get; set; }
    public int OverdueForContact { get; set; }
    public int FlaggedForMinister { get; set; }

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
