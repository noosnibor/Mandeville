namespace Mandeville.Web.Components.Shared;

/// <summary>Helpers for the round initials avatars (colour is stable per person).</summary>
public static class AvatarStyle
{
    #region Private Fields

    private static readonly string[] __colours =
        ["#be185d", "#0369a1", "#6366f1", "#0f766e", "#c2410c", "#7c3aed", "#b45309", "#0e7490"];

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
            "linear-gradient(135deg,#c7d2fe,#e0e7ff)", "linear-gradient(135deg,#99f6e4,#ccfbf1)",
            "linear-gradient(135deg,#fbcfe8,#fce7f3)", "linear-gradient(135deg,#bae6fd,#e0f2fe)"
        ];
        var h = key.GetHashCode();
        return covers[(h & int.MaxValue) % covers.Length];
    }

    #endregion
}
