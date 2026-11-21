namespace Mandeville.Library.Model;

/// <summary>
/// Turns what the team said they can do into a schedule: nobody is placed where they did not say yes, nobody gets more than
/// <see cref="EvangelismRules.MaxPerMonth"/> sessions or two sessions on the same Saturday, and everyone who is available gets at
/// least <see cref="EvangelismRules.MinPerMonth"/> before anyone gets a second. Pure logic: nothing here reads or writes the database.
/// </summary>
public static class EvangelismScheduler
{
    #region Input and result

    /// <summary>A session that may still need people.</summary>
    public sealed class SessionNeed
    {
        public required Guid Key { get; init; }
        public required DateTime Date { get; init; }
        public required string Area { get; init; }
        /// <summary>Total people the session asks for.</summary>
        public required int Capacity { get; init; }
        /// <summary>People already on it (anyone who has not declined).</summary>
        public HashSet<Guid> Members { get; init; } = [];
    }

    public sealed record Pick(Guid SessionKey, Guid MemberKey);

    /// <summary>What was planned: the people to add, available people who could not be placed, and sessions still short of people.</summary>
    public sealed record PlanResult(List<Pick> Picks, List<Guid> Unplaced, Dictionary<Guid, int> StillOpen);

    #endregion

    /// <summary>
    /// Plans the people to add. <paramref name="available"/> holds, for each member who answered, the (Saturday, area) pairs they ticked
    /// (members who said "not available" are simply left out). Sessions already have their current people in <see cref="SessionNeed.Members"/>.
    /// </summary>
    public static PlanResult Plan(IReadOnlyList<SessionNeed> sessions, IReadOnlyDictionary<Guid, HashSet<(DateTime Date, string Area)>> available)
    {
        var open = sessions.ToDictionary(s => s.Key, s => Math.Max(0, s.Capacity - s.Members.Count));
        var inSession = sessions.ToDictionary(s => s.Key, s => new HashSet<Guid>(s.Members));

        // How many sessions each member already has this month, and on which Saturdays
        var count = available.Keys.ToDictionary(m => m, _ => 0);
        var busy = available.Keys.ToDictionary(m => m, _ => new HashSet<DateTime>());
        foreach (var s in sessions)
            foreach (var m in s.Members)
            {
                if (!count.ContainsKey(m)) continue;
                count[m]++;
                busy[m].Add(s.Date.Date);
            }

        var picks = new List<Pick>();

        bool CanPlace(Guid m, SessionNeed s) =>
            open[s.Key] > 0
            && count[m] < EvangelismRules.MaxPerMonth
            && !inSession[s.Key].Contains(m)
            && !busy[m].Contains(s.Date.Date)
            && available[m].Contains((s.Date.Date, s.Area));

        void Place(Guid m, SessionNeed s)
        {
            picks.Add(new Pick(s.Key, m));
            open[s.Key]--;
            inSession[s.Key].Add(m);
            count[m]++;
            busy[m].Add(s.Date.Date);
        }

        // Step 1: everyone who is available gets their first session. The people with the fewest choices go first,
        // and each is put on the session that is emptiest, so the sessions fill evenly.
        foreach (var m in available.Keys.Where(k => count[k] < EvangelismRules.MinPerMonth).OrderBy(k => available[k].Count).ThenBy(k => k))
        {
            var best = sessions.Where(s => CanPlace(m, s))
                .OrderByDescending(s => (double)open[s.Key] / Math.Max(1, s.Capacity))
                .ThenBy(s => s.Date).ThenBy(s => s.Area)
                .FirstOrDefault();
            if (best is not null) Place(m, best);
        }

        // Step 2: fill what is still open, hardest sessions first (the fewest people said yes to them),
        // always taking the person who has served the least so far.
        var hardest = sessions
            .OrderBy(s => available.Values.Count(set => set.Contains((s.Date.Date, s.Area))))
            .ThenBy(s => s.Date).ThenBy(s => s.Area)
            .ToList();
        foreach (var s in hardest)
        {
            while (open[s.Key] > 0)
            {
                Guid? next = available.Keys
                    .Where(m => CanPlace(m, s))
                    .OrderBy(m => count[m]).ThenBy(m => available[m].Count).ThenBy(m => m)
                    .Select(m => (Guid?)m)
                    .FirstOrDefault();
                if (next is null) break;
                Place(next.Value, s);
            }
        }

        var unplaced = available.Keys.Where(m => count[m] < EvangelismRules.MinPerMonth && available[m].Count > 0).ToList();
        return new PlanResult(picks, unplaced, open.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}
