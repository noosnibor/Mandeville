namespace Mandeville.Library.Model;

/// <summary>
/// Dropdown values. Gender, age group, membership status and visitor status are locked (the app has rules that depend on them).
/// The other lists start with the defaults below and are edited in Setup; the saved values replace them when the app starts or a list is saved.
/// </summary>
public static class FixedLists
{
    public static readonly string[] Genders = ["Male", "Female"];
    public static string[] MaritalStatuses { get; private set; } = ["Single", "Married", "Divorced", "Widowed"];
    public static readonly string[] AgeGroups = ["Adult", "Child"];
    public static readonly string[] MembershipStatuses = ["Active", "Inactive"];
    public static readonly string[] VisitorStatuses = ["Visitor", "Regular Visitor", "Incoming Member", "Member"];
    public static string[] IncomingPaths { get; private set; } = ["New believer", "Returning", "Joining the church", "Partner"];
    public static string[] AltarReasons { get; private set; } = ["Salvation", "Prayer", "Recommitment", "Membership", "Partner"];

    /// <summary>True for the altar reason that puts a visitor into Incoming members ("Member" is the older spelling kept in existing data).</summary>
    #region Editable lists

    /// <summary>The lists that can be edited in Setup, by the name shown there.</summary>
    public static readonly string[] EditableNames = ["Marital status", "Incoming path", "Altar reason", "Payment method", "Evangelism role", "Care contact method", "Title"];

    /// <summary>Values the app has rules for. They stay in the list: they cannot be renamed or hidden.</summary>
    private static readonly Dictionary<string, string[]> __protected = new()
    {
        ["Marital status"] = ["Single", "Married", "Divorced"],
        ["Incoming path"] = ["New believer", "Returning", "Partner"],
        ["Altar reason"] = ["Prayer", "Membership"],
        ["Payment method"] = ["Cash"],
        ["Evangelism role"] = ["Caller"],
        ["Care contact method"] = [],
        ["Title"] = []
    };

    public static bool IsProtected(string list, string value) => __protected.TryGetValue(list, out var v) && v.Contains(value);

    /// <summary>The built-in values for an editable list (used until the list is saved in Setup).</summary>
    public static string[] Defaults(string list) => list switch
    {
        "Marital status" => ["Single", "Married", "Divorced", "Widowed"],
        "Incoming path" => ["New believer", "Returning", "Joining the church", "Partner"],
        "Altar reason" => ["Salvation", "Prayer", "Recommitment", "Membership", "Partner"],
        "Payment method" => ["Cash", "Bank transfer", "Online", "Cheque"],
        "Evangelism role" => ["Caller", "Prayer", "Team lead"],
        "Care contact method" => ["Phone call", "Visit", "WhatsApp", "Prayer", "Other"],
        "Title" => ["Mr.", "Mrs.", "Miss", "Ms.", "Dr.", "Pastor", "Minister", "Elder", "Deacon", "Deaconess", "Evangelist", "Brother", "Sister"],
        _ => []
    };

    /// <summary>Puts the shown (not hidden) values of one list into use, in the saved order.</summary>
    public static void Apply(string list, IReadOnlyList<string> values)
    {
        if (values.Count == 0) return;
        var v = values.ToArray();
        switch (list)
        {
            case "Marital status": MaritalStatuses = v; break;
            case "Incoming path": IncomingPaths = v; break;
            case "Altar reason": AltarReasons = v; break;
            case "Payment method": PaymentMethods = v; break;
            case "Evangelism role": EvangelismRoles = v; break;
            case "Care contact method": CareMethods = v; break;
            case "Title": Titles = v; break;
        }
    }

    #endregion

    public static bool IsMembershipReason(string? reason) => reason is "Membership" or "Member";
    public static string[] PaymentMethods { get; private set; } = ["Cash", "Bank transfer", "Online", "Cheque"];
    public static string[] EvangelismRoles { get; private set; } = ["Caller", "Prayer", "Team lead"];
    public static string[] CareMethods { get; private set; } = ["Phone call", "Visit", "WhatsApp", "Prayer", "Other"];
    /// <summary>Mr., Mrs., Pastor ... shown in the member form. Edited in Setup, Fixed lists.</summary>
    public static string[] Titles { get; private set; } = ["Mr.", "Mrs.", "Miss", "Ms.", "Dr.", "Pastor", "Minister", "Elder", "Deacon", "Deaconess", "Evangelist", "Brother", "Sister"];
    public static readonly string[] Months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
}
