namespace Mandeville.Web.Reports;

/// <summary>The kinds of period a report can cover.</summary>
public static class PeriodKinds
{
    public const string Week = "Week";
    public const string Month = "Month";
    public const string Quarter = "Quarter";
    public const string Year = "Year";
    public const string Custom = "Custom";

    public static readonly string[] All = [Week, Month, Quarter, Year, Custom];

    /// <summary>The first and last day of the period that contains <paramref name="anchor"/>. A week runs Monday to Sunday.</summary>
    public static (DateTime From, DateTime To) Range(string kind, DateTime anchor)
    {
        var d = anchor.Date;
        switch (kind)
        {
            case Week:
                var back = ((int)d.DayOfWeek + 6) % 7; // Monday = 0
                var monday = d.AddDays(-back);
                return (monday, monday.AddDays(6));
            case Quarter:
                var qStart = new DateTime(d.Year, ((d.Month - 1) / 3) * 3 + 1, 1);
                return (qStart, qStart.AddMonths(3).AddDays(-1));
            case Year:
                return (new DateTime(d.Year, 1, 1), new DateTime(d.Year, 12, 31));
            default:
                var mStart = new DateTime(d.Year, d.Month, 1);
                return (mStart, mStart.AddMonths(1).AddDays(-1));
        }
    }

    /// <summary>Moves a period forward or back (+1 / -1) and returns the new range.</summary>
    public static (DateTime From, DateTime To) Step(string kind, DateTime from, int direction) => kind switch
    {
        Week => Range(kind, from.AddDays(7 * direction)),
        Quarter => Range(kind, from.AddMonths(3 * direction)),
        Year => Range(kind, from.AddYears(direction)),
        _ => Range(Month, from.AddMonths(direction))
    };

    /// <summary>The big label shown for a period, for example "November 2026" or "Q4 2026".</summary>
    public static string Title(string kind, DateTime from, DateTime to) => kind switch
    {
        Week => $"Week of {from:d MMM yyyy}",
        Month => from.ToString("MMMM yyyy"),
        Quarter => $"Q{(from.Month - 1) / 3 + 1} {from:yyyy}",
        Year => from.ToString("yyyy"),
        _ => $"{from:d MMM yyyy} to {to:d MMM yyyy}"
    };
}

/// <summary>One line of the attendance table: a service, or a whole month when the period is long.</summary>
public class PeriodRow
{
    public string Label { get; set; } = string.Empty;
    public string Sub { get; set; } = string.Empty;
    public int Members { get; set; }
    public int Visitors { get; set; }
    public int Visiting { get; set; }
    public int Altar { get; set; }
    public int Total => Members + Visitors + Visiting;
}

/// <summary>Everything printed on one period report. Numbers only: no names and no care notes.</summary>
public class PeriodReport
{
    #region Period

    public string GroupName { get; set; } = string.Empty;
    public string Kind { get; set; } = PeriodKinds.Month;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public DateTime GeneratedOn { get; set; }
    public string Title => PeriodKinds.Title(Kind, From, To);
    public string RangeText => $"{From:d MMM yyyy} - {To:d MMM yyyy}";

    #endregion

    #region Attendance

    public int Services { get; set; }

    /// <summary>True when the rows are months (long periods) instead of single services.</summary>
    public bool RowsAreMonths { get; set; }
    public List<PeriodRow> Rows { get; set; } = [];

    public int MemberCheckIns { get; set; }
    public int VisitorVisits { get; set; }
    public int VisitingCheckIns { get; set; }
    public int AltarTotal { get; set; }
    public int TotalAttendance => MemberCheckIns + VisitorVisits + VisitingCheckIns;
    public int AvgMembers => Services == 0 ? 0 : (int)Math.Round((double)MemberCheckIns / Services);
    public int AvgVisitors => Services == 0 ? 0 : (int)Math.Round((double)VisitorVisits / Services);
    public int AvgVisiting => Services == 0 ? 0 : (int)Math.Round((double)VisitingCheckIns / Services);
    public int AvgTotal => Services == 0 ? 0 : (int)Math.Round((double)TotalAttendance / Services);
    public int PeakMembers { get; set; }
    public DateTime? PeakDate { get; set; }
    public int DifferentMembersPresent { get; set; }

    #endregion

    #region Visitors

    public int DifferentVisitors { get; set; }
    public int FirstTimeVisitors { get; set; }
    public int VisitorsMale { get; set; }
    public int VisitorsFemale { get; set; }
    public int VisitorsAdult { get; set; }
    public int VisitorsChild { get; set; }
    public int WentToAltar { get; set; }
    public List<(string Reason, int Count)> AltarReasons { get; set; } = [];
    public int Baptised { get; set; }

    #endregion

    #region Membership growth (dates that fall inside the period)

    public int Tier1 { get; set; }
    public int Tier2 { get; set; }
    public int Interviews { get; set; }
    public int Inaugurated { get; set; }
    public int MembersJoined { get; set; }

    #endregion

    #region Care and visiting members

    public int CareContacts { get; set; }
    public int MembersContacted { get; set; }
    public int FlagsRaised { get; set; }
    public int DifferentVisitingMembers { get; set; }
    public List<KeyValuePair<string, int>> VisitingByBranch { get; set; } = [];

    #endregion

    #region Evangelism

    public EvangelismNumbers Evangelism { get; set; } = new();
    public bool HasEvangelism => Evangelism.Sessions > 0;

    #endregion

    #region Finance

    /// <summary>Months fully inside the period whose collections are included, for example "Nov 2026".</summary>
    public List<string> CollectionMonths { get; set; } = [];

    /// <summary>When no whole month fits (a week, say) the collections shown are for this month so far, for context only.</summary>
    public bool CollectionsAreContext { get; set; }
    public string CollectionsLabel { get; set; } = string.Empty;
    public decimal DuesCollected { get; set; }
    public decimal OtherCollected { get; set; }
    public decimal TotalCollected => DuesCollected + OtherCollected;
    public decimal Expenses { get; set; }
    public List<(string Category, decimal Amount)> ExpenseCategories { get; set; } = [];

    /// <summary>Collected minus expenses; only worked out when real collections for whole months are included.</summary>
    public decimal? Net => CollectionsAreContext || CollectionMonths.Count == 0 ? null : TotalCollected - Expenses;

    #endregion

    public string FileName => $"Report {From:yyyy-MM-dd} to {To:yyyy-MM-dd}.pdf";
}
