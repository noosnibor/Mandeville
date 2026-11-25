using Mandeville.Library;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;

namespace Mandeville.Web.Reports;

/// <summary>
/// Works out the numbers for a week, month, quarter, year or custom period.
/// Everything here is counted from dates that fall inside the period, so a report for an old period
/// does not change as people move through the membership process later.
/// </summary>
public class PeriodReportBuilder(
    IConfiguration configuration,
    IServiceRepository serviceRepo,
    IVisitorRepository visitorRepo,
    IVisitorAttendanceRepository visitorAttendanceRepo,
    IMemberRepository memberRepo,
    IMemberAttendanceRepository memberAttendanceRepo,
    IVisitingAttendanceRepository visitingAttendanceRepo,
    IPastoralContactRepository contactRepo,
    IFinanceRepository financeRepo,
    IPaymentTypeRepository paymentTypeRepo,
    IExpenditureRepository expenditureRepo,
    IEvangelismServiceRepository evangelismServiceRepo)
{
    #region Private Fields

    /// <summary>Above this many services the attendance table shows one line per month instead of one per service.</summary>
    private const int MaxServiceRows = 14;

    private readonly IConfiguration __configuration = configuration;
    private readonly IServiceRepository __serviceRepo = serviceRepo;
    private readonly IVisitorRepository __visitorRepo = visitorRepo;
    private readonly IVisitorAttendanceRepository __visitorAttendanceRepo = visitorAttendanceRepo;
    private readonly IMemberRepository __memberRepo = memberRepo;
    private readonly IMemberAttendanceRepository __memberAttendanceRepo = memberAttendanceRepo;
    private readonly IVisitingAttendanceRepository __visitingAttendanceRepo = visitingAttendanceRepo;
    private readonly IPastoralContactRepository __contactRepo = contactRepo;
    private readonly IFinanceRepository __financeRepo = financeRepo;
    private readonly IPaymentTypeRepository __paymentTypeRepo = paymentTypeRepo;
    private readonly IExpenditureRepository __expenditureRepo = expenditureRepo;
    private readonly IEvangelismServiceRepository __evangelismServiceRepo = evangelismServiceRepo;

    #endregion

    /// <summary>Builds the report for the dates <paramref name="from"/> to <paramref name="to"/> (both included).</summary>
    public async Task<PeriodReport> BuildAsync(string kind, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        from = from.Date;
        to = to.Date;
        if (to < from) (from, to) = (to, from);

        var report = new PeriodReport
        {
            GroupName = __configuration["Church:GroupName"] ?? "WAFIF Mandeville",
            Kind = PeriodKinds.All.Contains(kind) ? kind : PeriodKinds.Custom,
            From = from,
            To = to,
            GeneratedOn = AppClock.Now
        };

        var services = (await __serviceRepo.SelectService(fromDate: from, toDate: to, active: true, cancellationToken: cancellationToken))
            .OrderBy(s => s.fdtmServiceDate).ThenBy(s => s.fstrServiceName).ToList();
        report.Services = services.Count;

        var members = await __memberRepo.SelectMember(active: true, cancellationToken: cancellationToken);

        var visitorAttendance = await AddAttendanceAsync(report, services, members, cancellationToken);
        await AddVisitorsAsync(report, visitorAttendance, members, cancellationToken);
        await AddCareAsync(report, from, to, cancellationToken);
        await AddEvangelismAsync(report, from, to, cancellationToken);
        await AddFinanceAsync(report, from, to, cancellationToken);
        return report;
    }

    #region Attendance

    private sealed record ServiceCount(ServiceModel Service, int Members, int Visiting, int Visitors, int Altar);

    /// <summary>Fills the attendance numbers and returns the visitor attendance inside the period for the visitor section.</summary>
    private async Task<List<VisitorAttendanceModel>> AddAttendanceAsync(PeriodReport r, List<ServiceModel> services, List<MemberModel> members, CancellationToken ct)
    {
        var keys = services.Select(s => s.flngKey).ToHashSet();
        var activeMembers = members.Where(m => m.fstrMembershipStatus == "Active").Select(m => m.flngKey).ToHashSet();

        // Only active members count as present, the same as the service report
        var memberAttendance = (await __memberAttendanceRepo.SelectMemberAttendance(active: true, cancellationToken: ct))
            .Where(a => keys.Contains(a.flngServiceKey) && activeMembers.Contains(a.flngMemberKey)).ToList();
        var visiting = (await __visitingAttendanceRepo.SelectVisitingAttendance(active: true, cancellationToken: ct))
            .Where(a => keys.Contains(a.flngServiceKey)).ToList();

        // Visitor attendance is saved by date, so a date with two services is counted once (on the first service)
        var serviceDates = services.Select(s => s.fdtmServiceDate.Date).ToHashSet();
        var visitors = (await __visitorRepo.SelectVisitor(active: true, cancellationToken: ct)).Select(v => v.flngKey).ToHashSet();
        var visitorAttendance = (await __visitorAttendanceRepo.SelectVisitorAttendance(active: true, cancellationToken: ct))
            .Where(a => visitors.Contains(a.flngVisitorKey) && serviceDates.Contains(a.fdtmServiceDate.Date)).ToList();

        var usedDates = new HashSet<DateTime>();
        var counts = new List<ServiceCount>();
        foreach (var s in services)
        {
            var day = s.fdtmServiceDate.Date;
            var firstOfDay = usedDates.Add(day);
            var onDay = firstOfDay ? visitorAttendance.Where(a => a.fdtmServiceDate.Date == day).ToList() : new List<VisitorAttendanceModel>();
            counts.Add(new ServiceCount(
                s,
                memberAttendance.Where(a => a.flngServiceKey == s.flngKey).Select(a => a.flngMemberKey).Distinct().Count(),
                visiting.Where(a => a.flngServiceKey == s.flngKey).Select(a => a.flngVisitingMemberKey).Distinct().Count(),
                onDay.Select(a => a.flngVisitorKey).Distinct().Count(),
                onDay.Where(a => !string.IsNullOrWhiteSpace(a.fstrAltarReason)).Select(a => a.flngVisitorKey).Distinct().Count()));
        }

        r.MemberCheckIns = counts.Sum(c => c.Members);
        r.VisitingCheckIns = counts.Sum(c => c.Visiting);
        r.VisitorVisits = counts.Sum(c => c.Visitors);
        r.AltarTotal = counts.Sum(c => c.Altar);
        r.DifferentMembersPresent = memberAttendance.Select(a => a.flngMemberKey).Distinct().Count();
        r.DifferentVisitingMembers = visiting.Select(a => a.flngVisitingMemberKey).Distinct().Count();
        r.VisitingByBranch = visiting
            .GroupBy(a => a.fstrBranch)
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Select(x => x.flngVisitingMemberKey).Distinct().Count()))
            .OrderByDescending(k => k.Value).ThenBy(k => k.Key)
            .ToList();

        var peak = counts.OrderByDescending(c => c.Members).FirstOrDefault();
        if (peak is not null && peak.Members > 0)
        {
            r.PeakMembers = peak.Members;
            r.PeakDate = peak.Service.fdtmServiceDate.Date;
        }

        if (counts.Count <= MaxServiceRows)
        {
            r.RowsAreMonths = false;
            r.Rows = counts.Select(c => new PeriodRow
            {
                Label = c.Service.fdtmServiceDate.ToString("ddd d MMM"),
                Sub = c.Service.fstrServiceName,
                Members = c.Members,
                Visitors = c.Visitors,
                Visiting = c.Visiting,
                Altar = c.Altar
            }).ToList();
        }
        else
        {
            r.RowsAreMonths = true;
            r.Rows = counts
                .GroupBy(c => new DateTime(c.Service.fdtmServiceDate.Year, c.Service.fdtmServiceDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new PeriodRow
                {
                    Label = g.Key.ToString("MMM yyyy"),
                    Sub = g.Count() == 1 ? "1 service" : $"{g.Count()} services",
                    Members = g.Sum(c => c.Members),
                    Visitors = g.Sum(c => c.Visitors),
                    Visiting = g.Sum(c => c.Visiting),
                    Altar = g.Sum(c => c.Altar)
                }).ToList();
        }

        return visitorAttendance;
    }

    #endregion

    #region Visitors and membership growth

    private async Task AddVisitorsAsync(PeriodReport r, List<VisitorAttendanceModel> attendance, List<MemberModel> members, CancellationToken ct)
    {
        var from = r.From;
        var to = r.To;
        var all = (await __visitorRepo.SelectVisitor(active: true, cancellationToken: ct)).ToDictionary(v => v.flngKey);

        var peopleKeys = attendance.Select(a => a.flngVisitorKey).Distinct().ToList();
        var people = peopleKeys.Where(all.ContainsKey).Select(k => all[k]).ToList();

        r.DifferentVisitors = people.Count;
        r.VisitorsMale = people.Count(v => v.fstrGender == "Male");
        r.VisitorsFemale = people.Count(v => v.fstrGender == "Female");
        r.VisitorsAdult = people.Count(v => v.fstrAgeGroup == "Adult");
        r.VisitorsChild = people.Count(v => v.fstrAgeGroup == "Child");

        // First-time = the very first recorded visit (ever) falls inside the period
        var everAttended = await __visitorAttendanceRepo.SelectVisitorAttendance(active: true, cancellationToken: ct);
        var firstVisit = everAttended.GroupBy(a => a.flngVisitorKey).ToDictionary(g => g.Key, g => g.Min(a => a.fdtmServiceDate.Date));
        r.FirstTimeVisitors = peopleKeys.Count(k => firstVisit.TryGetValue(k, out var d) && d >= from && d <= to);

        var altar = attendance.Where(a => !string.IsNullOrWhiteSpace(a.fstrAltarReason)).ToList();
        r.WentToAltar = altar.Select(a => a.flngVisitorKey).Distinct().Count();

        // The reason can change from one service to the next, so each person is counted once, under the reason from
        // their latest altar visit in the period. The reasons then add up to the number of people who went to the altar.
        var latest = altar
            .GroupBy(a => a.flngVisitorKey)
            .Select(g => g.OrderByDescending(a => a.fdtmServiceDate).ThenByDescending(a => a.fdtmWhen).First())
            .ToList();
        r.AltarReasons = FixedLists.AltarReasons
            .Select(reason => (reason, latest.Count(a => string.Equals(a.fstrAltarReason, reason, StringComparison.OrdinalIgnoreCase) || (reason == "Membership" && FixedLists.IsMembershipReason(a.fstrAltarReason)))))
            .ToList();

        // Membership progress is counted by the date each step happened, for everyone (partners are supporters, so they are left out)
        static string PathOf(VisitorModel v) => string.IsNullOrWhiteSpace(v.fstrPath) ? "New believer" : v.fstrPath;
        bool In(DateTime? d) => d is { } x && x.Date >= from && x.Date <= to;
        var process = all.Values.Where(v => PathOf(v) != "Partner").ToList();
        r.Baptised = all.Values.Count(v => In(v.fdtmBaptisedOn));
        r.Tier1 = process.Count(v => In(v.fdtmClassStartedOn));
        r.Tier2 = process.Count(v => In(v.fdtmTier2On));
        r.Interviews = process.Count(v => In(v.fdtmInterviewOn));
        r.Inaugurated = process.Count(v => In(v.fdtmInauguratedOn));

        // A membership reason at the altar is not membership. Only people who went through the process and were added
        // to the member list from the Incoming members page count (members added by hand are not "growth" from the process).
        var memberByKey = members.ToDictionary(m => m.flngKey);
        r.MembersJoined = all.Values.Count(v => v.flngMemberKey is Guid k && memberByKey.TryGetValue(k, out var m) && In(m.fdtmJoinDate));
    }

    #endregion

    #region Pastoral care

    private async Task AddCareAsync(PeriodReport r, DateTime from, DateTime to, CancellationToken ct)
    {
        var contacts = (await __contactRepo.SelectPastoralContact(cancellationToken: ct))
            .Where(c => c.fdtmContactDate.Date >= from && c.fdtmContactDate.Date <= to).ToList();
        r.CareContacts = contacts.Count;
        r.MembersContacted = contacts.Select(c => c.flngMemberKey).Distinct().Count();
        r.FlagsRaised = contacts.Count(c => c.fblnFlagForMinister);
    }

    #endregion

    #region Evangelism

    private async Task AddEvangelismAsync(PeriodReport r, DateTime from, DateTime to, CancellationToken ct)
    {
        var list = await __evangelismServiceRepo.SelectEvangelismStat(from, to, ct);
        r.Evangelism = new EvangelismNumbers
        {
            Sessions = list.Count,
            Scheduled = list.Sum(x => x.flngScheduled),
            Present = list.Sum(x => x.flngPresent),
            Flyers = list.Sum(x => x.flngFlyers),
            Ministered = list.Sum(x => x.flngMinistered),
            NewContacts = list.Sum(x => x.flngNewContacts),
            Calls = list.Sum(x => x.flngCalls),
            Reached = list.Sum(x => x.flngReached)
        };
    }

    #endregion

    #region Finance

    private async Task AddFinanceAsync(PeriodReport r, DateTime from, DateTime to, CancellationToken ct)
    {
        var paymentTypes = (await __paymentTypeRepo.SelectPaymentType(cancellationToken: ct)).ToDictionary(p => p.flngKey);
        bool IsDues(FinanceModel f) => paymentTypes.TryGetValue(f.flngPaymentTypeKey, out var p) && p.fblnIsDues;

        // Collections are recorded by month, so only months that fall completely inside the period are added up
        var months = new List<DateTime>();
        for (var m = new DateTime(from.Year, from.Month, 1); m <= to; m = m.AddMonths(1))
            if (m >= from && m.AddMonths(1).AddDays(-1) <= to) months.Add(m);

        if (months.Count > 0)
        {
            foreach (var m in months)
            {
                var rows = await __financeRepo.SelectFinance(month: m.Month, year: m.Year, active: true, cancellationToken: ct);
                r.DuesCollected += rows.Where(IsDues).Sum(f => f.fcurAmount);
                r.OtherCollected += rows.Where(f => !IsDues(f)).Sum(f => f.fcurAmount);
                r.CollectionMonths.Add(m.ToString("MMM yyyy"));
            }
            r.CollectionsLabel = r.CollectionMonths.Count == 1 ? r.CollectionMonths[0] : $"{r.CollectionMonths[0]} - {r.CollectionMonths[^1]}";
        }
        else
        {
            // Not a whole month (a week or a custom range): show the month so far for context only
            var m = new DateTime(to.Year, to.Month, 1);
            var rows = await __financeRepo.SelectFinance(month: m.Month, year: m.Year, active: true, cancellationToken: ct);
            r.DuesCollected = rows.Where(IsDues).Sum(f => f.fcurAmount);
            r.OtherCollected = rows.Where(f => !IsDues(f)).Sum(f => f.fcurAmount);
            r.CollectionsAreContext = true;
            r.CollectionsLabel = m.ToString("MMMM yyyy") + " so far";
        }

        var expenses = await __expenditureRepo.SelectExpenditure(fromDate: from, toDate: to, active: true, cancellationToken: ct);
        r.Expenses = expenses.Sum(e => e.fcurAmount);
        r.ExpenseCategories = expenses
            .GroupBy(e => string.IsNullOrWhiteSpace(e.fstrExpenseCategoryName) ? "Other" : e.fstrExpenseCategoryName!)
            .Select(g => (Category: g.Key, Amount: g.Sum(e => e.fcurAmount)))
            .OrderByDescending(x => x.Amount).Take(6).ToList();
    }

    #endregion
}
