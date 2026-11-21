using System.Net;

namespace Mandeville.Library.Model;

/// <summary>The fixed words and limits of the Evangelism module in one place.</summary>
public static class EvangelismRules
{
    #region Areas

    public const string Street = "Street Evangelism";
    public const string Tele = "Tele Evangelism";
    public const string Prayer = "Intercessory Prayer";

    public static readonly string[] Areas = [Street, Tele, Prayer];

    /// <summary>How many people a new session asks for, by area (the team lead can change it per session).</summary>
    public static int DefaultSlots(string area) => area switch { Street => 6, Tele => 4, Prayer => 3, _ => 4 };

    /// <summary>Where a quick-created session takes place, by area.</summary>
    public static string DefaultLocation(string area) => area switch { Tele => "By phone", Prayer => "At the church", _ => "" };

    /// <summary>The role written on the assignment, from the area.</summary>
    public static string RoleFor(string area) => area == Prayer ? "Prayer" : "Caller";

    public static string ShortArea(string area) => area switch { Street => "Street", Tele => "Tele", Prayer => "Prayer", _ => area };

    #endregion

    #region Limits

    /// <summary>Everyone who is available is given at least this many sessions in the month.</summary>
    public const int MinPerMonth = 1;

    /// <summary>Nobody is given more than this many sessions in a month (the team also travels to church every Sunday).</summary>
    public const int MaxPerMonth = 2;

    /// <summary>Unanswered calls before a contact is moved to Unreachable.</summary>
    public const int MaxFailedCalls = 3;

    /// <summary>Days an unreachable contact is kept before the personal details are removed.</summary>
    public const int KeepUnreachableDays = 90;

    #endregion

    #region Assignment status

    public const string Scheduled = "Scheduled";
    public const string Confirmed = "Confirmed";
    public const string Declined = "Declined";
    public const string Attended = "Attended";
    public const string Absent = "Absent";

    #endregion

    #region Contact status and call outcomes

    public const string ContactNew = "New";
    public const string ContactAssigned = "Assigned";
    public const string ContactContacted = "Contacted";
    public const string ContactFollowUp = "Follow-up";
    public const string ContactConnected = "Connected";
    public const string ContactUnreachable = "Unreachable";

    public static readonly string[] ContactStatuses = [ContactNew, ContactAssigned, ContactContacted, ContactFollowUp, ContactConnected, ContactUnreachable];

    public const string Reached = "Reached";
    public const string NoAnswer = "No answer";
    public const string WrongNumber = "Wrong number";
    public const string CallBack = "Call back";

    public static readonly string[] Outcomes = [Reached, NoAnswer, CallBack, WrongNumber];

    #endregion

    #region Helpers

    /// <summary>Every Saturday of a month.</summary>
    public static List<DateTime> Saturdays(DateTime month)
    {
        var first = new DateTime(month.Year, month.Month, 1);
        var list = new List<DateTime>();
        for (var d = first; d.Month == first.Month; d = d.AddDays(1))
            if (d.DayOfWeek == DayOfWeek.Saturday) list.Add(d);
        return list;
    }

    /// <summary>The first day of the month the date falls in.</summary>
    public static DateTime MonthOf(DateTime date) => new(date.Year, date.Month, 1);

    /// <summary>
    /// A wa.me link that opens WhatsApp with the message ready. Jamaican numbers (7 digits, or starting 876) get the 1 country code.
    /// Returns null when the number has too few digits.
    /// </summary>
    public static string? WhatsAppLink(string? phone, string text)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 7) digits = "1876" + digits;
        else if (digits.Length == 10 && digits.StartsWith("876")) digits = "1" + digits;
        return digits.Length < 10 ? null : $"https://wa.me/{digits}?text={WebUtility.UrlEncode(text)}";
    }

    /// <summary>A mailto link with the subject and message ready.</summary>
    public static string? MailLink(string? email, string subject, string body) =>
        string.IsNullOrWhiteSpace(email) ? null : $"mailto:{email.Trim()}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

    #endregion
}
