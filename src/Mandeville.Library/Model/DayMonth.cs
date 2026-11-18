namespace Mandeville.Library.Model;

/// <summary>
/// Birthdays and anniversaries are entered as a day and a month only. The database still keeps a full date, so when
/// the year is not known a fixed placeholder year is used (1904 is a leap year, so 29 February is allowed).
/// A real year that was entered earlier is kept as it is.
/// </summary>
public static class DayMonth
{
    /// <summary>The year stored when only a day and month are known.</summary>
    public const int UnknownYear = 1904;

    /// <summary>True when the date carries a real year (it was entered before, so ages and years married can be worked out).</summary>
    public static bool HasYear(DateTime date) => date.Year != UnknownYear;

    /// <summary>Builds a date from a day and month, keeping the given year (or the placeholder). A day past the end of the month is moved back (31 Feb becomes 28/29 Feb).</summary>
    public static DateTime Make(int day, int month, int? year = null)
    {
        var y = year ?? UnknownYear;
        return new DateTime(y, month, Math.Min(day, DateTime.DaysInMonth(y, month)));
    }
}
