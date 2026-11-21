using Mandeville.Library;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;

namespace Mandeville.Web.Reports;

/// <summary>Works out the numbers for a service report from the existing records.</summary>
public class ServiceReportBuilder(
    IConfiguration configuration,
    IServiceRepository serviceRepo,
    IVisitorRepository visitorRepo,
    IVisitorAttendanceRepository visitorAttendanceRepo,
    IMemberRepository memberRepo,
    IMemberAttendanceRepository memberAttendanceRepo,
    IPastoralAssignmentRepository assignmentRepo,
    IPastoralContactRepository contactRepo,
    IFinanceRepository financeRepo,
    IPaymentTypeRepository paymentTypeRepo,
    IExpenditureRepository expenditureRepo,
    IEvangelismServiceRepository evangelismServiceRepo,
    IEvangelismContactRepository evangelismContactRepo)
{
    #region Private Fields

    private const int CareGapDays = 21;

    private readonly IConfiguration __configuration = configuration;
    private readonly IServiceRepository __serviceRepo = serviceRepo;
    private readonly IVisitorRepository __visitorRepo = visitorRepo;
    private readonly IVisitorAttendanceRepository __visitorAttendanceRepo = visitorAttendanceRepo;
    private readonly IMemberRepository __memberRepo = memberRepo;
    private readonly IMemberAttendanceRepository __memberAttendanceRepo = memberAttendanceRepo;
    private readonly IPastoralAssignmentRepository __assignmentRepo = assignmentRepo;
    private readonly IPastoralContactRepository __contactRepo = contactRepo;
    private readonly IFinanceRepository __financeRepo = financeRepo;
    private readonly IPaymentTypeRepository __paymentTypeRepo = paymentTypeRepo;
    private readonly IExpenditureRepository __expenditureRepo = expenditureRepo;
    private readonly IEvangelismServiceRepository __evangelismServiceRepo = evangelismServiceRepo;
    private readonly IEvangelismContactRepository __evangelismContactRepo = evangelismContactRepo;

    #endregion

    /// <summary>Builds the report for one service. Returns null when the service does not exist.</summary>
    public async Task<ServiceReport?> BuildAsync(Guid serviceKey, CancellationToken cancellationToken = default)
    {
        var service = (await __serviceRepo.SelectService(key: serviceKey, cancellationToken: cancellationToken)).FirstOrDefault();
        if (service is null) return null;

        var date = service.fdtmServiceDate.Date;
        var report = new ServiceReport
        {
            ServiceKey = service.flngKey,
            GroupName = __configuration["Church:GroupName"] ?? "WAFIF Mandeville",
            ServiceName = service.fstrServiceName,
            ServiceDate = date,
            GeneratedOn = AppClock.Now
        };

        await AddVisitorsAsync(report, date, cancellationToken);
        await AddIncomingAsync(report, date, cancellationToken);
        await AddMembersAsync(report, service, date, cancellationToken);
        await AddEvangelismAsync(report, date, cancellationToken);
        await AddFinanceAsync(report, date, cancellationToken);
        return report;
    }

    #region Visitors

    private async Task AddVisitorsAsync(ServiceReport r, DateTime date, CancellationToken ct)
    {
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(serviceDate: date, active: true, cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(active: true, cancellationToken: ct)).ToDictionary(v => v.flngKey);

        var who = attendance
            .Where(a => visitors.ContainsKey(a.flngVisitorKey))
            .GroupBy(a => a.flngVisitorKey)
            .Select(g => visitors[g.Key])
            .ToList();

        r.VisitorsAttended = who.Count;
        r.VisitorsMale = who.Count(v => v.fstrGender == "Male");
        r.VisitorsFemale = who.Count(v => v.fstrGender == "Female");
        r.VisitorsAdult = who.Count(v => v.fstrAgeGroup == "Adult");
        r.VisitorsChild = who.Count(v => v.fstrAgeGroup == "Child");

        var altar = attendance.Where(a => !string.IsNullOrWhiteSpace(a.fstrAltarReason)).ToList();
        r.WentToAltar = altar.Select(a => a.flngVisitorKey).Distinct().Count();
        r.AltarReasons = FixedLists.AltarReasons
            .Select(reason => (reason, altar.Count(a => string.Equals(a.fstrAltarReason, reason, StringComparison.OrdinalIgnoreCase) || (reason == "Membership" && FixedLists.IsMembershipReason(a.fstrAltarReason)))))
            .ToList();

        // Follow-up events are counted from the service date until the next week, so a baptism or a call
        // made a few days after the service still belongs to that service.
        var until = date.AddDays(7);
        r.Baptised = who.Count(v => v.fdtmBaptisedOn is { } b && b.Date >= date && b.Date < until);
        r.StartedClass = who.Count(v => v.fdtmClassStartedOn is { } c && c.Date >= date && c.Date < until);
        r.Called = who.Count(v => v.fdtmLastContactDate is { } l && l.Date >= date);
        r.WaitingForConnector = who.Count(v => v.flngFollowUpMemberKey is null);
    }

    #endregion

    #region Incoming members

    private async Task AddIncomingAsync(ServiceReport r, DateTime date, CancellationToken ct)
    {
        var incoming = await __visitorRepo.SelectVisitor(status: "Incoming Member", active: true, cancellationToken: ct);

        static string PathOf(VisitorModel v) => string.IsNullOrWhiteSpace(v.fstrPath) ? "New believer" : v.fstrPath;

        r.IncomingTotal = incoming.Count;
        r.IncomingNewBelievers = incoming.Count(v => PathOf(v) == "New believer");
        r.IncomingReturning = incoming.Count(v => PathOf(v) == "Returning");
        r.IncomingJoining = incoming.Count(v => PathOf(v) == "Joining the church");
        r.IncomingPartners = incoming.Count(v => PathOf(v) == "Partner");
        r.IncomingWaitingForConnector = incoming.Count(v => v.flngFollowUpMemberKey is null);

        // Partners are supporters, so they do not go through the membership process.
        var process = incoming.Where(v => PathOf(v) != "Partner").ToList();
        r.Tier1Done = process.Count(v => v.fdtmClassStartedOn.HasValue);
        r.Tier2Done = process.Count(v => v.fdtmTier2On.HasValue);
        r.InterviewDone = process.Count(v => v.fdtmInterviewOn.HasValue);
        r.InauguratedDone = process.Count(v => v.fdtmInauguratedOn.HasValue);
        r.ReadyToConvert = r.InauguratedDone;

        var until = date.AddDays(7);
        bool InWeek(DateTime? d) => d is { } x && x.Date >= date && x.Date < until;
        r.WeekTier1 = process.Count(v => InWeek(v.fdtmClassStartedOn));
        r.WeekTier2 = process.Count(v => InWeek(v.fdtmTier2On));
        r.WeekInterview = process.Count(v => InWeek(v.fdtmInterviewOn));
        r.WeekInaugurated = process.Count(v => InWeek(v.fdtmInauguratedOn));
        r.WeekBaptised = process.Count(v => InWeek(v.fdtmBaptisedOn));
    }

    #endregion

    #region Members and pastoral care

    private async Task AddMembersAsync(ServiceReport r, ServiceModel service, DateTime date, CancellationToken ct)
    {
        var members = await __memberRepo.SelectMember(active: true, cancellationToken: ct);
        r.TotalMembers = members.Count;
        r.ActiveMembers = members.Count(m => m.fstrMembershipStatus == "Active");
        r.InactiveMembers = r.TotalMembers - r.ActiveMembers;

        var attendance = await __memberAttendanceRepo.SelectMemberAttendance(active: true, cancellationToken: ct);
        r.MembersPresent = attendance.Where(a => a.flngServiceKey == service.flngKey).Select(a => a.flngMemberKey).Distinct().Count();
        r.PresentPercent = r.ActiveMembers == 0 ? 0 : Math.Min(100, (int)Math.Round(100.0 * r.MembersPresent / r.ActiveMembers));

        // Not seen for 3+ weeks, counted as at the service date (skipped until check-ins exist)
        var cutoff = date.AddDays(-21);
        var before = attendance.Where(a => a.fdtmCheckedInAt.Date <= date).ToList();
        if (before.Count > 0)
        {
            var lastSeen = before.GroupBy(a => a.flngMemberKey).ToDictionary(g => g.Key, g => g.Max(a => a.fdtmCheckedInAt));
            r.NotSeen3Weeks = members.Count(m => m.fstrMembershipStatus == "Active"
                && (m.fdtmJoinDate is null || m.fdtmJoinDate.Value.Date <= cutoff)
                && (!lastSeen.TryGetValue(m.flngKey, out var seen) || seen.Date < cutoff));
        }

        // Pastoral care
        var careable = members.Where(m => m.fstrMembershipStatus == "Active" && !m.fblnCoreMember).ToList();
        r.Careable = careable.Count;
        r.Connected = careable.Count(m => m.flngCarerKey is not null);
        r.CoreMembers = members.Count(m => m.fblnCoreMember && m.fstrMembershipStatus == "Active");

        // Active members by title, in the order the titles are listed in Setup; anything else follows, then "No title"
        var active = members.Where(m => m.fstrMembershipStatus == "Active").ToList();
        var titles = FixedLists.Titles.ToList();
        r.MembersByTitle = active.Where(m => !string.IsNullOrWhiteSpace(m.fstrTitle))
            .GroupBy(m => m.fstrTitle!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => (Title: g.Key, Count: g.Count()))
            .OrderBy(t => TitleOrder(titles, t.Title))
            .ThenBy(t => t.Title)
            .ToList();
        var untitled = active.Count(m => string.IsNullOrWhiteSpace(m.fstrTitle));
        if (untitled > 0) r.MembersByTitle.Add(("No title", untitled));

        var assignments = await __assignmentRepo.SelectPastoralAssignment(cancellationToken: ct);
        var contacts = await __contactRepo.SelectPastoralContact(cancellationToken: ct);

        r.CareContactsThisWeek = contacts.Count(c => c.fdtmContactDate.Date > date.AddDays(-7) && c.fdtmContactDate.Date <= date);
        r.FlaggedForMinister = contacts.Count(c => c.fblnFlagForMinister && !c.fblnFlagResolved);
        r.OverdueForContact = assignments.Count(a =>
        {
            var last = contacts.Where(c => c.flngMemberKey == a.flngMemberKey && c.flngCoreMemberKey == a.flngCoreMemberKey
                                           && c.fdtmContactDate.Date <= date)
                               .OrderByDescending(c => c.fdtmContactDate).FirstOrDefault();
            if (last is null) return (date - a.fdtmAssignedOn.Date).TotalDays > CareGapDays;
            return (last.fdtmNextContactDate ?? last.fdtmContactDate.AddDays(CareGapDays)).Date < date;
        });
    }

    #endregion

    /// <summary>Position of a title in the Setup list; titles that are no longer listed go last.</summary>
    private static int TitleOrder(List<string> titles, string title)
    {
        var index = titles.FindIndex(x => string.Equals(x, title, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : int.MaxValue;
    }

    #region Evangelism

    private async Task AddEvangelismAsync(ServiceReport r, DateTime date, CancellationToken ct)
    {
        var monthStart = new DateTime(date.Year, date.Month, 1);
        var stats = await __evangelismServiceRepo.SelectEvangelismStat(monthStart, date, ct);

        static EvangelismNumbers Total(IEnumerable<EvangelismStatModel> rows)
        {
            var list = rows.ToList();
            return new EvangelismNumbers
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

        // "Last Saturday" is the week ending on the service date
        r.EvangelismSaturday = Total(stats.Where(x => x.fdtmServiceDate.Date > date.AddDays(-7) && x.fdtmServiceDate.Date <= date));
        r.EvangelismMonth = Total(stats);

        var contacts = await __evangelismContactRepo.SelectEvangelismContact(includePurged: true, cancellationToken: ct);
        r.ContactsConnected = contacts.Count(c => c.fstrStatus == EvangelismRules.ContactConnected);
        r.ContactsUnreachable = contacts.Count(c => c.fstrStatus == EvangelismRules.ContactUnreachable);
        r.ContactsActive = contacts.Count(c => c.fstrStatus != EvangelismRules.ContactUnreachable && c.fstrStatus != EvangelismRules.ContactConnected);
    }

    #endregion

    #region Finance

    private async Task AddFinanceAsync(ServiceReport r, DateTime date, CancellationToken ct)
    {
        r.FinanceMonth = date.ToString("MMMM yyyy");

        // Collections are entered per month, so the report shows the month so far
        var paymentTypes = (await __paymentTypeRepo.SelectPaymentType(cancellationToken: ct)).ToDictionary(p => p.flngKey);
        var collections = await __financeRepo.SelectFinance(month: date.Month, year: date.Year, active: true, cancellationToken: ct);
        r.DuesCollected = collections.Where(f => paymentTypes.TryGetValue(f.flngPaymentTypeKey, out var p) && p.fblnIsDues).Sum(f => f.fcurAmount);
        r.OtherCollected = collections.Where(f => !(paymentTypes.TryGetValue(f.flngPaymentTypeKey, out var p) && p.fblnIsDues)).Sum(f => f.fcurAmount);

        // Expenses since the previous service (a week back when there is none)
        var earlier = (await __serviceRepo.SelectService(fromDate: date.AddDays(-45), toDate: date.AddDays(-1), active: true, cancellationToken: ct))
            .OrderByDescending(s => s.fdtmServiceDate).FirstOrDefault();
        r.ExpensesFrom = earlier is null ? date.AddDays(-6) : earlier.fdtmServiceDate.Date.AddDays(1);

        var expenses = await __expenditureRepo.SelectExpenditure(fromDate: r.ExpensesFrom, toDate: date, active: true, cancellationToken: ct);
        r.Expenses = expenses.Sum(e => e.fcurAmount);
        r.TopExpenses = expenses
            .GroupBy(e => string.IsNullOrWhiteSpace(e.fstrExpenseCategoryName) ? "Other" : e.fstrExpenseCategoryName!)
            .Select(g => (Category: g.Key, Amount: g.Sum(e => e.fcurAmount)))
            .OrderByDescending(x => x.Amount).Take(3).ToList();
    }

    #endregion
}
