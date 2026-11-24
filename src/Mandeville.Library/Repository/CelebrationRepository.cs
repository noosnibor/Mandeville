using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Upcoming birthdays and anniversaries, and the record of which cards were sent.</summary>
public partial interface ICelebrationRepository
{
    /// <summary>Birthdays and anniversaries from today up to <paramref name="days"/> days ahead.</summary>
    Task<List<CelebrationModel>> SelectUpcoming(int days = 7, CancellationToken cancellationToken = default);

    /// <summary>Cards marked as sent in the given year.</summary>
    Task<List<CelebrationLogModel>> SelectSent(int year, CancellationToken cancellationToken = default);

    Task<ToastMessageModel> MarkSent(CelebrationModel celebration, string? caption, string channel, string? sentTo, string? who, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> UndoSent(Guid logKey, string? who, CancellationToken cancellationToken = default);

    /// <summary>Cards prepared in the Queue tab, waiting to be sent; pass a status (for example "Queued") to filter, and <paramref name="includeImage"/> when the uploaded image is needed.</summary>
    Task<List<CelebrationQueueModel>> SelectQueue(string? status = null, bool includeImage = false, CancellationToken cancellationToken = default);

    /// <summary>Saves the card, the extra message and the uploaded image to be emailed by the daily job on the celebration day.</summary>
    Task<ToastMessageModel> QueueCard(CelebrationModel celebration, string? message, byte[]? image, string? imageContentType, string? who, CancellationToken cancellationToken = default);

    /// <summary>Changes the status of a queued card (Queued, Sent or Failed) and keeps the reason when it failed.</summary>
    Task<ToastMessageModel> SetQueueStatus(Guid queueKey, string status, string? error, string? who, CancellationToken cancellationToken = default);

    /// <summary>Takes a card out of the queue (kept as history, not erased).</summary>
    Task<ToastMessageModel> CancelQueued(Guid queueKey, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class CelebrationRepository(IDataAccess dataAccess, IMemberRepository memberRepository) : ICelebrationRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;
    private readonly IMemberRepository __memberRepository = memberRepository;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<CelebrationModel>> SelectUpcoming(int days = 7, CancellationToken cancellationToken = default)
    {
        var members = await __memberRepository.SelectMember(membershipStatus: "Active", cancellationToken: cancellationToken);
        var today = AppClock.Today.Date;
        var log = await SelectSentLogAsync(null, cancellationToken);
        var queue = (await SelectQueue(null, false, cancellationToken)).Where(q => q.fblnActive && q.fstrStatus is "Queued" or "Failed").ToList();

        var result = new List<CelebrationModel>();
        var byKey = members.ToDictionary(m => m.flngKey);

        foreach (var m in members.Where(m => !m.fblnDontAnnounce))
        {
            // ----- Birthday -----
            if (m.fdtmDateOfBirth.HasValue)
            {
                var next = NextOccurrence(m.fdtmDateOfBirth.Value, today);
                var away = (next - today).Days;
                if (away <= days)
                {
                    var item = new CelebrationModel
                    {
                        Type = "Birthday",
                        MemberKey = m.flngKey,
                        DisplayName = m.FullName,
                        ShortName = m.fstrFirstname,
                        GreetingName = m.fstrFirstname,
                        Gender = m.fstrGender ?? string.Empty,
                        Emails = ValidEmails(m.fstrEmailAddress),
                        Date = next,
                        DaysAway = away,
                        // No count when only the day and month are known
                        Years = DayMonth.HasYear(m.fdtmDateOfBirth.Value) ? next.Year - m.fdtmDateOfBirth.Value.Year : 0
                    };
                    Apply(item, log, queue);
                    result.Add(item);
                }
            }

            // ----- Anniversary (married members only; a couple of two members gives one card) -----
            if (m.fstrMaritalStatus == "Married" && m.fdtmAnniversaryDate.HasValue)
            {
                byKey.TryGetValue(m.flngSpouseMemberKey ?? Guid.Empty, out var spouse);

                // The partner who sorts first carries the card
                if (spouse is not null && !spouse.fblnDontAnnounce && spouse.fstrMaritalStatus == "Married"
                    && string.CompareOrdinal(spouse.flngKey.ToString(), m.flngKey.ToString()) < 0)
                    continue;
                if (spouse is not null && spouse.fblnDontAnnounce)
                    continue;

                var next = NextOccurrence(m.fdtmAnniversaryDate.Value, today);
                var away = (next - today).Days;
                var years = DayMonth.HasYear(m.fdtmAnniversaryDate.Value) ? next.Year - m.fdtmAnniversaryDate.Value.Year : 0;
                // years can be 0 when the date entered is this year's wedding date; the card then has no year count
                if (away > days || years < 0) continue;

                var item = new CelebrationModel
                {
                    Type = "Anniversary",
                    MemberKey = m.flngKey,
                    SpouseKey = spouse?.flngKey,
                    DisplayName = CoupleName(m, spouse),
                    ShortName = CoupleName(m, spouse),
                    GreetingName = CoupleGreeting(m, spouse),
                    Emails = ValidEmails(m.fstrEmailAddress, spouse?.fstrEmailAddress),
                    Date = next,
                    DaysAway = away,
                    Years = years
                };
                Apply(item, log, queue);
                result.Add(item);
            }
        }

        return result.OrderBy(r => r.DaysAway).ThenBy(r => r.DisplayName).ToList();
    }

    /// <inheritdoc />
    public async Task<List<CelebrationLogModel>> SelectSent(int year, CancellationToken cancellationToken = default) =>
        await SelectSentLogAsync(year, cancellationToken);

    private async Task<List<CelebrationLogModel>> SelectSentLogAsync(int? year, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngYear", year);
        parameters.Add("@fblnActive", true);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<CelebrationLogModel>("mandeville.sp_SelectCelebrationLog", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Calculation helpers

    /// <summary>The next date (today or later) on which the month and day of <paramref name="original"/> fall.</summary>
    private static DateTime NextOccurrence(DateTime original, DateTime today)
    {
        var date = InYear(original, today.Year);
        return date < today ? InYear(original, today.Year + 1) : date;
    }

    /// <summary>29 February is celebrated on 28 February in years that are not leap years.</summary>
    private static DateTime InYear(DateTime original, int year)
    {
        var day = original.Month == 2 && original.Day == 29 && !DateTime.IsLeapYear(year) ? 28 : original.Day;
        return new DateTime(year, original.Month, day);
    }

    /// <summary>Marks the card as sent when a log row already exists for that member (or spouse), type and year.</summary>
    private static void Apply(CelebrationModel item, List<CelebrationLogModel> log, List<CelebrationQueueModel> queue)
    {
        var waiting = queue.FirstOrDefault(q => q.fstrType == item.Type && q.flngYear == item.Date.Year
            && (q.flngMemberKey == item.MemberKey || (item.SpouseKey.HasValue && q.flngMemberKey == item.SpouseKey.Value)));
        item.QueueKey = waiting?.flngKey;
        item.QueueStatus = waiting?.fstrStatus;
        item.QueueError = waiting?.fstrError;

        var entry = log.FirstOrDefault(l => l.fstrType == item.Type && l.flngYear == item.Date.Year
            && (l.flngMemberKey == item.MemberKey || (item.SpouseKey.HasValue && l.flngMemberKey == item.SpouseKey.Value)));
        item.Sent = entry is not null;
        item.LogKey = entry?.flngKey;
        item.SentChannel = entry?.fstrChannel;
        item.SentTo = entry?.fstrSentTo;
        item.SentOn = entry?.fdtmWhen;
    }

    /// <summary>"Grant and Joy Williams" when they share a surname, otherwise both full names.</summary>
    private static string CoupleName(MemberModel m, MemberModel? spouse)
    {
        if (spouse is not null)
        {
            return string.Equals(m.fstrLastname, spouse.fstrLastname, StringComparison.OrdinalIgnoreCase)
                ? $"{m.fstrFirstname} and {spouse.fstrFirstname} {m.fstrLastname}"
                : $"{m.FullName} and {spouse.FullName}";
        }

        return string.IsNullOrWhiteSpace(m.fstrSpouseName) ? m.FullName : $"{m.FullName} and {m.fstrSpouseName}";
    }

    /// <summary>"Paul and Deifre": the first names used in the greeting of an anniversary.</summary>
    private static string CoupleGreeting(MemberModel m, MemberModel? spouse)
    {
        if (spouse is not null) return $"{m.fstrFirstname} and {spouse.fstrFirstname}";
        return string.IsNullOrWhiteSpace(m.fstrSpouseName) ? m.fstrFirstname : $"{m.fstrFirstname} and {m.fstrSpouseName.Split(' ')[0]}";
    }

    /// <summary>Keeps only addresses that are well formed, without duplicates.</summary>
    private static List<string> ValidEmails(params string?[] addresses) =>
        addresses.Where(a => !string.IsNullOrWhiteSpace(a) && System.Net.Mail.MailAddress.TryCreate(a.Trim(), out _))
                 .Select(a => a!.Trim())
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> MarkSent(CelebrationModel celebration, string? caption, string channel, string? sentTo, string? who, CancellationToken cancellationToken = default)
    {
        if (celebration is null)
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", Guid.NewGuid());
            parameters.Add("@flngMemberKey", celebration.MemberKey);
            parameters.Add("@fstrType", celebration.Type);
            parameters.Add("@flngYear", celebration.Date.Year);
            parameters.Add("@fstrDisplayName", celebration.DisplayName);
            parameters.Add("@fstrCaption", string.IsNullOrWhiteSpace(caption) ? null : caption.Trim()[..Math.Min(500, caption.Trim().Length)]);
            parameters.Add("@fstrChannel", channel is "Email" or "WhatsApp" ? channel : "Manual");
            parameters.Add("@fstrSentTo", string.IsNullOrWhiteSpace(sentTo) ? null : sentTo.Length > 250 ? sentTo[..250] : sentTo);
            parameters.Add("@fblnActive", true);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddCelebrationLog", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Marked as sent");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> UndoSent(Guid logKey, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", logKey);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteCelebrationLog", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Moved back to upcoming");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    #endregion

    #region Queue Operations

    /// <inheritdoc />
    public async Task<List<CelebrationQueueModel>> SelectQueue(string? status = null, bool includeImage = false, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrStatus", status);
        parameters.Add("@fblnIncludeImage", includeImage);
        parameters.Add("@fblnActive", true);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<CelebrationQueueModel>("mandeville.sp_SelectCelebrationQueue", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> QueueCard(CelebrationModel celebration, string? message, byte[]? image, string? imageContentType, string? who, CancellationToken cancellationToken = default)
    {
        if (celebration is null || celebration.Emails.Count == 0)
            return Invalid();

        try
        {
            var text = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
            if (text is { Length: > 500 }) text = text[..500];

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", Guid.NewGuid());
            parameters.Add("@flngMemberKey", celebration.MemberKey);
            parameters.Add("@flngSpouseMemberKey", celebration.SpouseKey);
            parameters.Add("@fstrType", celebration.Type);
            parameters.Add("@flngYear", celebration.Date.Year);
            parameters.Add("@fdtmDueDate", celebration.Date.Date);
            parameters.Add("@fstrDisplayName", celebration.DisplayName);
            parameters.Add("@fstrGreetingName", celebration.GreetingName);
            parameters.Add("@fstrGender", string.IsNullOrWhiteSpace(celebration.Gender) ? null : celebration.Gender);
            parameters.Add("@flngYears", celebration.Years);
            parameters.Add("@fstrEmails", string.Join(",", celebration.Emails));
            parameters.Add("@fstrMessage", text);
            parameters.Add("@fbinImage", image is { Length: > 0 } ? image : null, DbType.Binary);
            parameters.Add("@fstrImageContentType", image is { Length: > 0 } ? imageContentType : null);
            parameters.Add("@fblnActive", true);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddCelebrationQueue", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Added to the queue");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> SetQueueStatus(Guid queueKey, string status, string? error, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", queueKey);
            parameters.Add("@fstrStatus", status);
            parameters.Add("@fstrError", string.IsNullOrWhiteSpace(error) ? null : error.Length > 500 ? error[..500] : error);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SetCelebrationQueueStatus", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Queue updated");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> CancelQueued(Guid queueKey, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", queueKey);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteCelebrationQueue", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Removed from the queue");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    #endregion
}
