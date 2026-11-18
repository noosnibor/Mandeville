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

/// <summary>Everything the side panel shows for one role.</summary>
public sealed class RolePanelData
{
    public string Title { get; init; } = string.Empty;
    public List<StatTile> Tiles { get; init; } = [];
    public List<StatBar> Bars { get; init; } = [];
    public List<AttentionItem> Attention { get; init; } = [];
}

/// <summary>One member in a team lead's "My team" table.</summary>
public sealed record TeamMemberRow(string Name, bool[] Attended, DateTime? LastContact, bool FollowUp);

/// <summary>A department the signed-in person leads, with its members' activity.</summary>
public sealed class TeamData
{
    public Guid DepartmentKey { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public int PresentLastSunday { get; init; }
    public int MissedThree { get; init; }
    public List<TeamMemberRow> Members { get; init; } = [];
}

#endregion

/// <summary>
/// Works out the numbers shown in the side panel on the Visitors, Connections and Prayerline and Pastoral Care pages,
/// and the "My team" section for team leads. Everything is calculated from data the app already keeps.
/// </summary>
public sealed class RoleStatsService(
    IServiceRepository serviceRepo,
    IVisitorRepository visitorRepo,
    IVisitorAttendanceRepository visitorAttendanceRepo,
    IVisitorNoteRepository noteRepo,
    ICelebrationRepository celebrationRepo,
    IPastoralAssignmentRepository assignmentRepo,
    IPastoralContactRepository contactRepo,
    IMemberAttendanceRepository memberAttendanceRepo,
    IMemberDepartmentRepository memberDepartmentRepo,
    IMemberRepository memberRepo,
    IDepartmentRepository departmentRepo,
    IConfiguration configuration)
{
    #region Private Fields

    private readonly IServiceRepository __serviceRepo = serviceRepo;
    private readonly IVisitorRepository __visitorRepo = visitorRepo;
    private readonly IVisitorAttendanceRepository __visitorAttendanceRepo = visitorAttendanceRepo;
    private readonly IVisitorNoteRepository __noteRepo = noteRepo;
    private readonly ICelebrationRepository __celebrationRepo = celebrationRepo;
    private readonly IPastoralAssignmentRepository __assignmentRepo = assignmentRepo;
    private readonly IPastoralContactRepository __contactRepo = contactRepo;
    private readonly IMemberAttendanceRepository __memberAttendanceRepo = memberAttendanceRepo;
    private readonly IMemberDepartmentRepository __memberDepartmentRepo = memberDepartmentRepo;
    private readonly IMemberRepository __memberRepo = memberRepo;
    private readonly IDepartmentRepository __departmentRepo = departmentRepo;
    private readonly IConfiguration __configuration = configuration;

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

    /// <summary>Visitors this week, who still needs a thank-you, upcoming celebrations and this month's thank-yous.</summary>
    public async Task<RolePanelData> GetMembersCareAsync(CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var attendance = await __visitorAttendanceRepo.SelectVisitorAttendance(cancellationToken: ct);
        var notes = await __noteRepo.SelectVisitorNote(cancellationToken: ct);
        var visitors = (await __visitorRepo.SelectVisitor(cancellationToken: ct)).ToDictionary(v => v.flngKey);
        var days = int.TryParse(__configuration["Celebrations:DaysBefore"], out var d) && d is >= 0 and <= 60 ? d : 3;
        var upcoming = (await __celebrationRepo.SelectUpcoming(days, ct)).Count(c => !c.Sent);

        bool Thanked(VisitorAttendanceModel a) => notes.Any(n => n.flngVisitorKey == a.flngVisitorKey && ThankYouNotes.IsFor(n.fstrNote, a.fdtmServiceDate));

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
                new($"Celebrations in {days} days", upcoming.ToString()),
                new("Thanked this month", thankedThisMonth.ToString())
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

    #region Team lead (My team)

    /// <summary>The departments this member leads, each with the members' attendance over the last four services and the last contact.</summary>
    public async Task<List<TeamData>> GetMyTeamsAsync(Guid memberKey, CancellationToken ct = default)
    {
        var mine = (await __memberDepartmentRepo.SelectMemberDepartment(memberKey: memberKey, cancellationToken: ct)).Where(l => l.fblnLead).ToList();
        if (mine.Count == 0) return [];

        var today = AppClock.Today;
        var services = (await __serviceRepo.SelectService(toDate: today, cancellationToken: ct))
            .Where(s => s.fdtmServiceDate.Date <= today).OrderByDescending(s => s.fdtmServiceDate).Take(4).Reverse().ToList();
        var attendance = await __memberAttendanceRepo.SelectMemberAttendance(cancellationToken: ct);
        var lastContact = (await __contactRepo.SelectPastoralContact(cancellationToken: ct))
            .GroupBy(c => c.flngMemberKey).ToDictionary(g => g.Key, g => g.Max(c => c.fdtmContactDate));
        var departments = (await __departmentRepo.SelectDepartment(cancellationToken: ct)).ToDictionary(x => x.flngKey);

        var teams = new List<TeamData>();
        foreach (var link in mine)
        {
            var members = await __memberRepo.SelectMember(membershipStatus: "Active", departmentKey: link.flngDepartmentKey, cancellationToken: ct);
            var rows = new List<TeamMemberRow>();
            foreach (var m in members)
            {
                var present = services.Select(s => attendance.Any(a => a.flngMemberKey == m.flngKey && a.flngServiceKey == s.flngKey)).ToArray();
                lastContact.TryGetValue(m.flngKey, out var last);
                DateTime? lastDate = last == default ? null : last;
                var missedThree = present.Length >= 3 && present.TakeLast(3).All(p => !p);
                var longSince = lastDate is null || (today - lastDate.Value.Date).Days > 30;
                rows.Add(new TeamMemberRow(m.FullName, present, lastDate, missedThree || (longSince && present.Length > 0 && !present[^1])));
            }

            teams.Add(new TeamData
            {
                DepartmentKey = link.flngDepartmentKey,
                DepartmentName = departments.TryGetValue(link.flngDepartmentKey, out var dept) ? dept.fstrName : link.fstrDepartmentName ?? "Team",
                PresentLastSunday = rows.Count(r => r.Attended.Length > 0 && r.Attended[^1]),
                MissedThree = rows.Count(r => r.Attended.Length >= 3 && r.Attended.TakeLast(3).All(p => !p)),
                Members = rows
            });
        }
        return teams;
    }

    /// <summary>Shown as "9 days ago" in the table.</summary>
    public static string Ago(DateTime? date) => AgoText(date);

    #endregion
}
