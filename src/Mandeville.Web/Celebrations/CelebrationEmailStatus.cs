using System.Collections.Concurrent;

namespace Mandeville.Web.Celebrations;

/// <summary>
/// Remembers (in memory) which cards could not be emailed, so the Celebrations tab can show "Email failed"
/// until you try again. It is cleared when the app restarts.
/// </summary>
public class CelebrationEmailStatus
{
    private readonly ConcurrentDictionary<string, string> __failures = new();

    /// <summary>The reason the last email for this card failed, or null.</summary>
    public string? FailureFor(string cardId) => __failures.TryGetValue(cardId, out var reason) ? reason : null;

    public void RecordFailure(string cardId, string reason) => __failures[cardId] = reason;

    public void Clear(string cardId) => __failures.TryRemove(cardId, out _);
}
