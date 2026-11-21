namespace Mandeville.Web.Components.Shared;

/// <summary>Helpers for the round initials avatars (colour is stable per person).</summary>
public static class AvatarStyle
{
    #region Private Fields

    private static readonly string[] __colours =
        ["#db2777", "#0284c7", "#6366f1", "#0d9488", "#ea580c", "#7c3aed", "#d97706", "#0891b2"];

    #endregion

    #region Public Methods

    /// <summary>Up to two capital letters from the first and last name.</summary>
    public static string Initials(string? first, string? last)
    {
        var a = string.IsNullOrWhiteSpace(first) ? "" : first.Trim()[..1];
        var b = string.IsNullOrWhiteSpace(last) ? "" : last.Trim()[..1];
        var text = (a + b).ToUpperInvariant();
        return text.Length == 0 ? "?" : text;
    }

    /// <summary>A colour picked from the person's key, so it never changes between visits.</summary>
    public static string Colour(Guid key) => __colours[(key.GetHashCode() & int.MaxValue) % __colours.Length];

    /// <summary>Soft cover gradient used behind a profile card avatar.</summary>
    public static string Cover(Guid key)
    {
        string[] covers =
        [
            "linear-gradient(135deg,#818cf8,#c7d2fe)", "linear-gradient(135deg,#2dd4bf,#99f6e4)",
            "linear-gradient(135deg,#f472b6,#fbcfe8)", "linear-gradient(135deg,#38bdf8,#bae6fd)"
        ];
        var h = key.GetHashCode();
        return covers[(h & int.MaxValue) % covers.Length];
    }

    #endregion
}
