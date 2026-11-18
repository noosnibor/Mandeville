namespace Mandeville.Web.Security;

/// <summary>
/// The name of whoever is signed in, flowed per request / circuit event through an AsyncLocal so the
/// repositories-callers can keep writing <see cref="AppConstants.Who"/> into fstrWho.
/// </summary>
public static class AppUser
{
    private static readonly AsyncLocal<string?> __current = new();

    /// <summary>Name of the signed-in person for the current call, or null when nobody is signed in.</summary>
    public static string? Current
    {
        get => __current.Value;
        set => __current.Value = value;
    }
}
