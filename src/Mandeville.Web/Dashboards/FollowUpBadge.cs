using System.Security.Claims;
using Mandeville.Library;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;
using Mandeville.Web.Security;

namespace Mandeville.Web.Dashboards;

/// <summary>
/// How many follow-up calls are due, for the badge on the Connections menu item and the notice on that page.
/// A team member sees the calls on their own list; the lead and administrators see the whole team's.
/// One instance per signed-in browser tab (scoped), so a change on the page updates the menu straight away.
/// </summary>
/// <summary>One call that is due: who to call, which team member owes the call and which service the visitor came to.</summary>
public sealed record DueCall(string Visitor, string? Member, string Service, int DaysLate);

public sealed class FollowUpBadge(IVisitorRepository visitorRepo, IVisitorAttendanceRepository attendanceRepo, IServiceRepository serviceRepo)
{
    #region Private Fields

    private readonly IVisitorRepository __visitorRepo = visitorRepo;
    private readonly IVisitorAttendanceRepository __attendanceRepo = attendanceRepo;
    private readonly IServiceRepository __serviceRepo = serviceRepo;
    private ClaimsPrincipal? __user;

    #endregion

    #region State

    /// <summary>Number of follow-up calls due today or earlier.</summary>
    public int Count { get; private set; }

    /// <summary>The calls that are due, longest overdue first.</summary>
    public IReadOnlyList<DueCall> Calls { get; private set; } = [];

    /// <summary>Raised after the numbers change so the menu can redraw.</summary>
    public event Action? Changed;

    #endregion

    #region Refresh

    /// <summary>Works out the numbers for this person (remembered for later refreshes).</summary>
    public async Task RefreshAsync(ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        if (user is not null) __user = user;
        if (__user is null || !__user.CanPage(PageKeys.Connect)) return;

        try
        {
            var today = AppClock.Today;
            Guid? mine = null;
            if (__user.OwnOnly(PageKeys.Connect))
            {
                // Only their own list; without a linked member there is nothing to count
                if (!Guid.TryParse(__user.FindFirstValue(AppClaims.MemberKey), out var key)) { Apply(0, []); return; }
                mine = key;
            }

            var visitors = await __visitorRepo.SelectVisitor(followUpMemberKey: mine, cancellationToken: cancellationToken);
            var due = visitors
                .Where(v => (v.fstrStatus is "Visitor" or "Regular Visitor") && v.flngFollowUpMemberKey is not null
                            && v.fdtmNextContactDate is { } next && next.Date <= today)
                .OrderBy(v => v.fdtmNextContactDate)
                .ToList();

            // Which service each visitor came to: the latest visit with an altar reason, otherwise the latest visit
            var services = (await __serviceRepo.SelectService(cancellationToken: cancellationToken)).ToList();
            var calls = new List<DueCall>();
            foreach (var v in due.Take(20))
            {
                var visits = await __attendanceRepo.SelectVisitorAttendance(visitorKey: v.flngKey, cancellationToken: cancellationToken);
                var visit = visits.Where(a => !string.IsNullOrEmpty(a.fstrAltarReason)).OrderByDescending(a => a.fdtmServiceDate).FirstOrDefault()
                            ?? visits.OrderByDescending(a => a.fdtmServiceDate).FirstOrDefault();
                var service = visit is null ? "no service recorded"
                    : $"{services.FirstOrDefault(s => s.fdtmServiceDate.Date == visit.fdtmServiceDate.Date)?.fstrServiceName ?? "Service"} {visit.fdtmServiceDate:dd MMM}";
                calls.Add(new DueCall(v.FullName, v.fstrFollowUpName, service, (today - v.fdtmNextContactDate!.Value.Date).Days));
            }
            Apply(due.Count, calls);
        }
        catch (OperationCanceledException) { }
        catch
        {
            // The badge is a convenience: if the numbers cannot be loaded, the menu simply shows no badge.
            Apply(0, []);
        }
    }

    private void Apply(int count, IReadOnlyList<DueCall> calls)
    {
        Count = count;
        Calls = calls;
        Changed?.Invoke();
    }

    #endregion
}
