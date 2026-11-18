namespace Mandeville.Web;

/// <summary>Values shared across the app.</summary>
public static class AppConstants
{
    /// <summary>Written to fstrWho on every add / update / delete.</summary>
    public static string Who => Mandeville.Web.Security.AppUser.Current ?? "System";

    /// <summary>How often the usher's checked-in list pulls new check-ins while check-in is open.</summary>
    public static readonly TimeSpan LiveRefreshEvery = TimeSpan.FromSeconds(5);
}
