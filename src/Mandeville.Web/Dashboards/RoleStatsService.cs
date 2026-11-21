using Mandeville.Library;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;

namespace Mandeville.Web.Dashboards;

#region Result shapes

/// <summary>One number box in the side panel.</summary>
public sealed record StatTile(string Label, string Value, bool Warn = false);

/// <summary>A progress line ("8 of 12").</summary>
public sealed record StatBar(string Label, int Done, int Total);

/// <summary>One person in the "Needs attention" list.</summary>
public sealed record AttentionItem(string Text, string Tag, bool Warn);

/// <summary>One service in a "per service" list: how many people came, and how many were male and female.</summary>
public sealed record ServiceCount(string Label, int Total, int Male, int Female);

/// <summary>Everything the side panel shows for one role.</summary>
public sealed class RolePanelData
{
    public string Title { get; init; } = string.Empty;
    public List<StatTile> Tiles { get; init; } = [];
    public List<StatBar> Bars { get; init; } = [];
    /// <summary>An optional figure shown in its own strip under the tiles.</summary>
    public StatTile? Highlight { get; init; }
    public List<AttentionItem> Attention { get; init; } = [];
    /// <summary>An optional list of the services in one month with their visitor counts, and the heading for it.</summary>
    public List<ServiceCount> PerService { get; init; } = [];
    public string PerServiceTitle { get; init; } = string.Empty;
    /// <summary>The month's total across the services in <see cref="PerService"/>.</summary>
    public ServiceCount? PerServiceTotal { get; init; }
}

#endregion

/// <summary>
/// Works out the numbers shown in the side panel on the Visitors, Connections and Prayerline and Pastoral Care pages.
/// Everything is calculated from data the app already keeps.
/// </summary>
public sealed class RoleStatsService(
    IServiceRepository serviceRepo,
    IVisitorRepository visitorRepo,
    IVisitorAttendanceRepository visitorAttendanceRepo,
    IVisitorNoteRepository noteRepo,
    IPastoralAssignmentRepository assignmentRepo,
    IPastoralContactRepository contactRepo)
{
    #region Private Fields

    private readonly IServiceRepository __serviceRepo = serviceRepo;
    private readonly IVisitorRepository __visitorRepo = visitorRepo;
    private readonly IVisitorAttendanceRepository __visitorAttendanceRepo = visitorAttendanceRepo;
    private readonly IVisitorNoteRepository __noteRepo = noteRepo;
    private readonly IPastoralAssignmentRepository __assignmentRepo = assignmentRepo;
    private readonly IPastoralContactRepository __contactRepo = contactRepo;

    #endregion

    #region Helpers

    private static string DaysText(int days) => days <= 0 ? "today" : days == 1 ? "1 day" : $"{days} days";

    private static string AgoText(DateTime? date)
    {
        if (date is null) return "never";
        var days = (AppClock.Today - date.Value.Date).Days;
        return days <= 0 ? "today" : days < 14 ? $"{days} days ago" : $"{days / 7} weeks ago";
    }

    #endregion

    #region Members care (Visitors page)

    /// <summary>Visitors this week, who still needs a thank-you and this month's thank-yous.</summary>
    public async Task<RolePanelData> GetMembersCareAsync(CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var notes = await __noteRepo.SelectVisitorNote(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).ToDictionary(v => v.flngKey);
        // Visits of deleted visitors no longer count
        attendance = attendance.Where(a => visitors.ContainsKey(a.flngVisitorKey)).ToList();

        VisitorNoteModel? LatestNote(VisitorAttendanceModel a) => ThankYouNotes.Latest(notes.Where(n => n.flngVisitorKey == a.flngVisitorKey), a.fdtmServiceDate);
        bool Thanked(VisitorAttendanceModel a) => LatestNote(a) is { } n && ThankYouNotes.IsFor(n.fstrNote, a.fdtmServiceDate);

        var thisWeek = attendance.Where(a => a.fdtmServiceDate.Date >= today.AddDays(-6)).Select(a => a.flngVisitorKey).Distinct().Count();
        var recent = attendance.Where(a => a.fdtmServiceDate.Date >= today.AddDays(-14)).ToList();
        var waiting = recent.Where(a => !Thanked(a)).OrderBy(a => a.fdtmServiceDate).ToList();

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthVisits = attendance.Where(a => a.fdtmServiceDate.Date >= monthStart).ToList();
        var thankedThisMonth = monthVisits.Count(Thanked);

        var attention = waiting.Take(4)
            .Select(a => new AttentionItem($"{a.VisitorName} - not thanked", DaysText((today - a.fdtmServiceDate.Date).Days), true)).ToList();

        // Visitors on their 3rd visit or more who have not started the membership class yet
        attention.AddRange(visitors.Values
            .Where(v => v.flngVisitCount >= 3 && v.fdtmClassStartedOn is null && v.fstrStatus != "Member")
            .Take(3).Select(v => new AttentionItem($"{v.FullName} - {v.flngVisitCount} visits, invite to class", "new", false)));

        return new RolePanelData
        {
            Title = "Members care",
            Tiles =
            [
                new("Visitors this week", thisWeek.ToString()),
                new("Awaiting thank you", waiting.Count.ToString(), waiting.Count > 0),
                new("Thanked this month", thankedThisMonth.ToString())
            ],
            Bars = [new("Thanked this month", thankedThisMonth, Math.Max(monthVisits.Count, thankedThisMonth))],
            Attention = attention
        };
    }

    #endregion

    #region Visitors page (one summary per tab)

    /// <summary>The date of the latest service on or before today, which is the "current service".</summary>
    private async Task<DateTime> CurrentServiceDateAsync(CancellationToken ct)
    {
        var today = AppClock.Today;
        var services = await __serviceRepo.SelectService(cancellationToken: ct);
        return services.Where(s => s.fdtmServiceDate.Date <= today).Select(s => s.fdtmServiceDate.Date).DefaultIfEmpty(today).Max();
    }

    /// <summary>Add Visitor tab: how many visitors (and how many men and women) came to the service, altar calls, and visitors per recent service.</summary>
    public async Task<RolePanelData> GetVisitorAddAsync(DateTime? chosenService = null, CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var serviceDate = chosenService ?? await CurrentServiceDateAsync(ct);
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).ToDictionary(v => v.flngKey);
        // Visits of deleted visitors no longer count
        attendance = attendance.Where(a => visitors.ContainsKey(a.flngVisitorKey)).ToList();

        // Ask for the service date directly, the same way the visitor list does, so the counts always match the list.
        var atService = await __visitorAttendanceRepo.SelectVisitorAttendance(serviceDate: serviceDate, cancellationToken: ct);
        atService = atService.Where(a => visitors.ContainsKey(a.flngVisitorKey)).ToList();
        var altar = atService.Where(a => !string.IsNullOrEmpty(a.fstrAltarReason)).GroupBy(a => a.flngVisitorKey).Select(g => g.First()).ToList();
        var newThisMonth = attendance.GroupBy(a => a.flngVisitorKey).Count(g => g.Min(a => a.fdtmServiceDate).Date >= monthStart);

        bool FollowedUp(VisitorAttendanceModel a) => visitors.TryGetValue(a.flngVisitorKey, out var v) && v.fdtmLastContactDate is { } c && c.Date >= a.fdtmServiceDate.Date;
        var followedUp = altar.Count(FollowedUp);

        // Each visitor counts once for the service, whatever their age
        var visitorKeys = atService.Select(a => a.flngVisitorKey).Distinct().ToList();
        bool IsGender(Guid key, string gender) => string.Equals(visitors[key].fstrGender, gender, StringComparison.OrdinalIgnoreCase);
        var maleCount = visitorKeys.Count(k => IsGender(k, "Male"));
        var femaleCount = visitorKeys.Count(k => IsGender(k, "Female"));

        // The services in the month of the chosen service (up to today), newest first, with zero for a service nobody was recorded at
        var serviceDates = (await __serviceRepo.SelectService(cancellationToken: ct))
            .Select(s => s.fdtmServiceDate.Date)
            .Where(d => d <= today && d.Year == serviceDate.Year && d.Month == serviceDate.Month)
            .Distinct().OrderByDescending(d => d).ToList();
        var perService = serviceDates.Select(date =>
        {
            var keys = attendance.Where(a => a.fdtmServiceDate.Date == date).Select(a => a.flngVisitorKey).Distinct().ToList();
            return new ServiceCount(date.ToString("ddd d MMM"), keys.Count, keys.Count(k => IsGender(k, "Male")), keys.Count(k => IsGender(k, "Female")));
        }).ToList();

        var attention = altar.Where(a => !FollowedUp(a))
            .Select(a => new AttentionItem($"{a.VisitorName} - altar call, not followed up", DaysText((today - a.fdtmServiceDate.Date).Days), true)).Take(4).ToList();
        attention.AddRange(atService.Select(a => a.flngVisitorKey).Distinct()
            .Where(k => visitors.TryGetValue(k, out var v) && string.IsNullOrWhiteSpace(v.fstrPhoneNumber))
            .Take(3).Select(k => new AttentionItem($"{visitors[k].FullName} - no phone number", "new", false)));

        return new RolePanelData
        {
            Title = "Visitor",
            Tiles =
            [
                new("Visitors at this service", visitorKeys.Count.ToString()),
                new("Altar calls waiting", (altar.Count - followedUp).ToString(), altar.Count - followedUp > 0),
                new("Male", maleCount.ToString()),
                new("Female", femaleCount.ToString())
            ],
            Highlight = new("New this month - first-time visitors since 1 " + monthStart.ToString("MMM"), newThisMonth.ToString()),
            Bars = [new("Altar calls followed up", followedUp, altar.Count)],
            Attention = attention,
            PerService = perService,
            PerServiceTitle = "Visitors per service - " + serviceDate.ToString("MMMM"),
            PerServiceTotal = new ServiceCount(serviceDate.ToString("MMMM") + " total", perService.Sum(x => x.Total), perService.Sum(x => x.Male), perService.Sum(x => x.Female))
        };
    }

    /// <summary>Returning Visitor tab: who is coming back, who is close to joining and who has gone quiet.</summary>
    public async Task<RolePanelData> GetVisitorReturningAsync(CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).Where(v => v.fstrStatus != "Member").ToList();
        // Visits of deleted visitors no longer count
        var activeKeys = visitors.Select(v => v.flngKey).ToHashSet();
        attendance = attendance.Where(a => activeKeys.Contains(a.flngVisitorKey)).ToList();

        var seenThisMonth = attendance.Where(a => a.fdtmServiceDate.Date >= monthStart).Select(a => a.flngVisitorKey).Distinct().ToList();
        var returnedThisMonth = visitors.Count(v => v.flngVisitCount >= 2 && seenThisMonth.Contains(v.flngKey));
        var second = visitors.Count(v => v.flngVisitCount == 2);
        var thirdPlus = visitors.Where(v => v.flngVisitCount >= 3).ToList();
        var quiet = visitors.Where(v => v.flngVisitCount >= 2 && v.fdtmLastAttended is { } l && (today - l.Date).Days > 21).OrderBy(v => v.fdtmLastAttended).ToList();

        var attention = quiet.Take(4)
            .Select(v => new AttentionItem($"{v.FullName} - last seen {AgoText(v.fdtmLastAttended)}", DaysText((today - v.fdtmLastAttended!.Value.Date).Days), true)).ToList();
        attention.AddRange(thirdPlus.Where(v => v.fdtmClassStartedOn is null).Take(3)
            .Select(v => new AttentionItem($"{v.FullName} - {v.flngVisitCount} visits, invite to join", $"{v.flngVisitCount} visits", false)));

        return new RolePanelData
        {
            Title = "Returning",
            Tiles =
            [
                new("Returned this month", returnedThisMonth.ToString()),
                new("On their second visit", second.ToString()),
                new("Third visit or more", thirdPlus.Count.ToString()),
                new("Not seen in 3 weeks", quiet.Count.ToString(), quiet.Count > 0)
            ],
            Bars = [new("Returned this month", returnedThisMonth, Math.Max(seenThisMonth.Count, returnedThisMonth))],
            Attention = attention
        };
    }

    /// <summary>Thank You tab: who still needs a thank-you message and how this month is going.</summary>
    public async Task<RolePanelData> GetVisitorThankAsync(CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var notes = await __noteRepo.SelectVisitorNote(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).ToDictionary(v => v.flngKey);
        // Visits of deleted visitors no longer count
        attendance = attendance.Where(a => visitors.ContainsKey(a.flngVisitorKey)).ToList();

        VisitorNoteModel? LatestNote(VisitorAttendanceModel a) => ThankYouNotes.Latest(notes.Where(n => n.flngVisitorKey == a.flngVisitorKey), a.fdtmServiceDate);
        bool Thanked(VisitorAttendanceModel a) => LatestNote(a) is { } n && ThankYouNotes.IsFor(n.fstrNote, a.fdtmServiceDate);
        bool NotSent(VisitorAttendanceModel a) => LatestNote(a) is { } n && ThankYouNotes.IsFailedFor(n.fstrNote, a.fdtmServiceDate);
        bool NoPhone(VisitorAttendanceModel a) => visitors.TryGetValue(a.flngVisitorKey, out var v) && string.IsNullOrWhiteSpace(v.fstrPhoneNumber);

        var thisWeek = attendance.Where(a => a.fdtmServiceDate.Date >= today.AddDays(-6)).Select(a => a.flngVisitorKey).Distinct().Count();
        var waiting = attendance.Where(a => a.fdtmServiceDate.Date >= today.AddDays(-14) && !Thanked(a)).OrderBy(a => a.fdtmServiceDate).ToList();
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthVisits = attendance.Where(a => a.fdtmServiceDate.Date >= monthStart).ToList();
        var thankedThisMonth = monthVisits.Count(Thanked);

        // Messages that failed come first so they are not forgotten
        var attention = waiting.OrderByDescending(NotSent).ThenBy(a => a.fdtmServiceDate).Take(6)
            .Select(a => new AttentionItem(
                NotSent(a) ? $"{a.VisitorName} - thank-you not sent, call instead" : NoPhone(a) ? $"{a.VisitorName} - no phone number" : $"{a.VisitorName} - not thanked",
                NotSent(a) ? "not sent" : NoPhone(a) ? "can't send" : DaysText((today - a.fdtmServiceDate.Date).Days), true)).ToList();

        return new RolePanelData
        {
            Title = "Thank-you",
            Tiles =
            [
                new("Awaiting thank you", waiting.Count.ToString(), waiting.Count > 0),
                new("Thanked this month", thankedThisMonth.ToString()),
                new("Can't send (no phone)", waiting.Count(NoPhone).ToString(), waiting.Any(NoPhone)),
                new("Visitors this week", thisWeek.ToString())
            ],
            Bars = [new("Thanked this month", thankedThisMonth, Math.Max(monthVisits.Count, thankedThisMonth))],
            Attention = attention
        };
    }

    #endregion

    #region Connections and Prayerline

    /// <summary>Follow-ups due, prayer requests waiting for contact, and new visitors.</summary>
    public async Task<RolePanelData> GetConnectionsAsync(CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).ToDictionary(v => v.flngKey);
        // Visits of deleted visitors no longer count
        attendance = attendance.Where(a => visitors.ContainsKey(a.flngVisitorKey)).ToList();

        var due = visitors.Values.Where(v => v.fstrStatus != "Member" && v.fdtmNextContactDate is { } n && n.Date <= today).ToList();

        // First-ever visit this month
        var firstVisit = attendance.GroupBy(a => a.flngVisitorKey).ToDictionary(g => g.Key, g => g.Min(a => a.fdtmServiceDate).Date);
        var newKeys = firstVisit.Where(f => f.Value >= monthStart).Select(f => f.Key).ToList();

        // Calls are meant to happen within 24 hours of the visit
        bool CalledInTime(Guid key) => visitors.TryGetValue(key, out var v) && v.fdtmLastContactDate is { } c && c.Date >= firstVisit[key] && (c.Date - firstVisit[key]).Days <= 1;
        var followedUp = newKeys.Count(CalledInTime);
        var newOverdue = newKeys.Where(k => !(visitors.TryGetValue(k, out var v) && v.fdtmLastContactDate is { } c && c.Date >= firstVisit[k]) && (today - firstVisit[k]).Days > 1).ToList();

        // Prayer requests are the altar calls with the reason "Prayer"
        var prayer = attendance.Where(a => a.fstrAltarReason == "Prayer").ToList();
        var prayerWeek = prayer.Count(a => a.fdtmServiceDate.Date >= today.AddDays(-6));
        bool Contacted(VisitorAttendanceModel a) => visitors.TryGetValue(a.flngVisitorKey, out var v) && v.fdtmLastContactDate is { } c && c.Date >= a.fdtmServiceDate.Date;
        var recentPrayer = prayer.Where(a => a.fdtmServiceDate.Date >= today.AddDays(-30)).ToList();
        var waiting = recentPrayer.Where(a => !Contacted(a)).OrderBy(a => a.fdtmServiceDate).ToList();
        var inTime = recentPrayer.Count(a => visitors.TryGetValue(a.flngVisitorKey, out var v) && v.fdtmLastContactDate is { } c && c.Date >= a.fdtmServiceDate.Date && (c.Date - a.fdtmServiceDate.Date).Days <= 1);

        var overdueCalls = waiting.Count(a => (today - a.fdtmServiceDate.Date).Days > 1) + newOverdue.Count;

        var attention = due.OrderBy(v => v.fdtmNextContactDate)
            .Select(v => (Sort: v.fdtmNextContactDate!.Value, Item: new AttentionItem($"{v.FullName} - follow-up call", DaysText((today - v.fdtmNextContactDate!.Value.Date).Days), (today - v.fdtmNextContactDate!.Value.Date).Days > 0)))
            .Concat(waiting.Select(a => (Sort: a.fdtmServiceDate, Item: new AttentionItem($"{a.VisitorName} - prayer request", DaysText((today - a.fdtmServiceDate.Date).Days), (today - a.fdtmServiceDate.Date).Days > 1))))
            .OrderBy(x => x.Sort).Take(6).Select(x => x.Item).ToList();

        return new RolePanelData
        {
            Title = "Connections and Prayerline",
            Tiles =
            [
                new("Follow-ups due", due.Count.ToString(), due.Count > 0),
                new("Calls overdue (over 24 hours)", overdueCalls.ToString(), overdueCalls > 0),
                new("New visitors this month", newKeys.Count.ToString()),
                new("Prayer requests this week", prayerWeek.ToString())
            ],
            Bars =
            [
                new("New visitors called within 24 hours", followedUp, newKeys.Count),
                new("Prayer requests called within 24 hours", inTime, recentPrayer.Count)
            ],
            Attention = attention
        };
    }

    /// <summary>Team Lead tab: the altar calls at the chosen service, who still needs a connector and this month's new visitors.</summary>
    public async Task<RolePanelData> GetConnectLeadAsync(DateTime? service = null, CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var date = (service ?? today).Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).ToDictionary(v => v.flngKey);
        // Visits of deleted visitors no longer count
        attendance = attendance.Where(a => visitors.ContainsKey(a.flngVisitorKey)).ToList();

        // The same people the Team Lead list shows: still visitors (not staged as incoming members, not deleted)
        bool StillVisitor(VisitorAttendanceModel a) => visitors.TryGetValue(a.flngVisitorKey, out var v) && (v.fstrStatus is "Visitor" or "Regular Visitor");
        var altar = attendance.Where(a => a.fdtmServiceDate.Date == date && !string.IsNullOrEmpty(a.fstrAltarReason) && StillVisitor(a))
            .GroupBy(a => a.flngVisitorKey).Select(g => g.First()).ToList();
        bool Unassigned(VisitorAttendanceModel a) => !(visitors.TryGetValue(a.flngVisitorKey, out var v) && v.flngFollowUpMemberKey is not null);
        var waiting = altar.Where(Unassigned).ToList();

        var newThisMonth = attendance.GroupBy(a => a.flngVisitorKey).Count(g => g.Min(a => a.fdtmServiceDate).Date >= monthStart);
        var prayerWeek = attendance.Count(a => a.fstrAltarReason == "Prayer" && a.fdtmServiceDate.Date >= today.AddDays(-6) && StillVisitor(a));

        return new RolePanelData
        {
            Title = "Team lead",
            Tiles =
            [
                new("Altar calls this service", altar.Count.ToString()),
                new("Waiting to be assigned", waiting.Count.ToString(), waiting.Count > 0),
                new("New visitors this month", newThisMonth.ToString()),
                new("Prayer requests this week", prayerWeek.ToString())
            ],
            Bars = [new("Assigned this service", altar.Count - waiting.Count, altar.Count)],
            Attention = waiting.Take(6)
                .Select(a => new AttentionItem($"{a.VisitorName} - not assigned", DaysText((today - a.fdtmServiceDate.Date).Days), true)).ToList()
        };
    }

    /// <summary>My Team Members tab: the people assigned to one team member and the calls they owe.</summary>
    public async Task<RolePanelData> GetConnectTeamAsync(Guid? memberKey, CancellationToken ct = default)
    {
        var today = AppClock.Today;
        List<VisitorModel> mine = memberKey is null
            ? []
            : (await __visitorRepo.SelectVisitor(followUpMemberKey: memberKey, cancellationToken: ct))
                .Where(v => v.fstrStatus is "Visitor" or "Regular Visitor").ToList();

        var due = mine.Where(v => v.fdtmNextContactDate is { } n && n.Date <= today).OrderBy(v => v.fdtmNextContactDate).ToList();
        var overdue = due.Count(v => (today - v.fdtmNextContactDate!.Value.Date).Days > 1);
        var calledWeek = mine.Count(v => v.fdtmLastContactDate is { } c && c.Date >= today.AddDays(-6));
        var contacted = mine.Count(v => v.fdtmLastContactDate is not null);

        return new RolePanelData
        {
            Title = "My team",
            Tiles =
            [
                new("Assigned to them", mine.Count.ToString()),
                new("Follow-ups due", due.Count.ToString(), due.Count > 0),
                new("Calls overdue (over 24 hours)", overdue.ToString(), overdue > 0),
                new("Called this week", calledWeek.ToString())
            ],
            Bars = [new("Contacted at least once", contacted, mine.Count)],
            Attention = due.Take(6)
                .Select(v => new AttentionItem($"{v.FullName} - follow-up call", DaysText((today - v.fdtmNextContactDate!.Value.Date).Days), (today - v.fdtmNextContactDate!.Value.Date).Days > 1)).ToList()
        };
    }

    #endregion

    #region Incoming members

    /// <summary>Incoming members page: who is ready to join, who has no connector and how far people have got.</summary>
    public async Task<RolePanelData> GetIncomingAsync(CancellationToken ct = default)
    {
        var list = await __visitorRepo.SelectVisitor(status: "Incoming Member", active: true, cancellationToken: ct);

        // The stage follows the dates: the latest date filled in decides it (0 = not started, 4 = inauguration)
        static int Stage(VisitorModel v) =>
            v.fdtmInauguratedOn.HasValue ? 4 : v.fdtmInterviewOn.HasValue ? 3 : v.fdtmTier2On.HasValue ? 2 : v.fdtmClassStartedOn.HasValue ? 1 : 0;

        var ready = list.Where(v => Stage(v) == 4).ToList();
        var noConnector = list.Where(v => v.flngFollowUpMemberKey is null).ToList();
        var started = list.Count(v => Stage(v) >= 1);

        var attention = ready.Select(v => new AttentionItem($"{v.FullName} - ready to add to members", "now", true))
            .Concat(noConnector.Where(v => Stage(v) < 4).Select(v => new AttentionItem($"{v.FullName} - no connector", "waiting", true)))
            .Take(6).ToList();

        return new RolePanelData
        {
            Title = "Incoming",
            Tiles =
            [
                new("In progress", list.Count.ToString()),
                new("Ready to add to members", ready.Count.ToString(), ready.Count > 0),
                new("Waiting for a connector", noConnector.Count.ToString(), noConnector.Count > 0),
                new("Not started", list.Count(v => Stage(v) == 0).ToString())
            ],
            Bars = [new("Reached Tier 1 or beyond", started, list.Count)],
            Attention = attention
        };
    }

    #endregion

    #region Pastoral care

    /// <summary>Contacts, contacts due and minister flags. Pass the carer's key to limit the numbers to their own caseload.</summary>
    public async Task<RolePanelData> GetPastoralAsync(Guid? coreMemberKey, CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var assignments = await __assignmentRepo.SelectPastoralAssignment(coreMemberKey: coreMemberKey, cancellationToken: ct);
        var contacts = await __contactRepo.SelectPastoralContact(coreMemberKey: coreMemberKey, cancellationToken: ct);
        var flagged = await __contactRepo.SelectPastoralContact(coreMemberKey: coreMemberKey, flaggedUnresolved: true, cancellationToken: ct);

        var latest = contacts.GroupBy(c => c.flngMemberKey).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.fdtmContactDate).First());
        var caseload = assignments.GroupBy(a => a.flngMemberKey).Select(g => g.First()).ToList();

        var overdue = caseload.Where(a => latest.TryGetValue(a.flngMemberKey, out var l) && l.fdtmNextContactDate is { } n && n.Date <= today).ToList();
        var stale = caseload.Where(a => !latest.TryGetValue(a.flngMemberKey, out var l) || (today - l.fdtmContactDate.Date).Days > 30).ToList();
        var contactedRecently = caseload.Count - stale.Count;

        var attention = flagged.Take(3).Select(c => new AttentionItem($"{c.fstrMemberName} - flagged for minister", "open", true)).ToList();
        attention.AddRange(overdue.Take(3).Select(a => new AttentionItem($"{a.fstrMemberName} - next contact overdue", DaysText((today - latest[a.flngMemberKey].fdtmNextContactDate!.Value.Date).Days), true)));
        attention.AddRange(stale.Take(3).Select(a => new AttentionItem($"{a.fstrMemberName} - not contacted in 30 days", latest.TryGetValue(a.flngMemberKey, out var l) ? DaysText((today - l.fdtmContactDate.Date).Days) : "never", true)));

        return new RolePanelData
        {
            Title = "Pastoral care",
            Tiles =
            [
                new("Contacts this month", contacts.Count(c => c.fdtmContactDate.Date >= monthStart).ToString()),
                new("Contacts due", overdue.Count.ToString(), overdue.Count > 0),
                new("Flagged for minister", flagged.Count.ToString(), flagged.Count > 0),
                new("Not contacted in 30 days", stale.Count.ToString())
            ],
            Bars = [new("Members contacted in the last 30 days", contactedRecently, caseload.Count)],
            Attention = attention
        };
    }

    #endregion
}
