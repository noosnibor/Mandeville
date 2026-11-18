namespace Mandeville.Library;

/// <summary>
/// The app's clock. Servers (Azure) run on UTC, so "now" and "today" are always worked out in
/// Bogota time (UTC-5, no daylight saving) instead of the server's own time zone.
/// </summary>
public static class AppClock
{
    #region Private Fields

    private static readonly TimeZoneInfo __zone = FindZone();

    #endregion

    #region Public Properties

    /// <summary>Current date and time in Bogota.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, __zone);

    /// <summary>Today's date (midnight) in Bogota.</summary>
    public static DateTime Today => Now.Date;

    #endregion

    #region Private Methods

    /// <summary>Finds Bogota by its IANA id (Linux, Azure App Service) or Windows id; falls back to a fixed UTC-5 zone.</summary>
    private static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "America/Bogota", "SA Pacific Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { /* try the next id */ }
            catch (InvalidTimeZoneException) { /* try the next id */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Bogota", TimeSpan.FromHours(-5), "Bogota", "Bogota");
    }

    #endregion
}
