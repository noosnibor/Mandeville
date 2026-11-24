namespace Mandeville.Web.Components.Shared;

/// <summary>Builds tel:, sms: and WhatsApp links from a stored phone number (Jamaican numbers get the 1876 prefix).</summary>
public static class ContactLinks
{
    /// <summary>Digits only, in international form for WhatsApp. Null when the number is too short to use.</summary>
    public static string? International(string? phone)
    {
        var d = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (d.StartsWith("00")) d = d[2..];
        if (d.Length == 7) return "1876" + d;
        if (d.Length == 10) return "1" + d;
        return d.Length >= 11 ? d : null;
    }

    private static string Dialable(string? phone) => new string((phone ?? "").Where(ch => char.IsDigit(ch) || ch == '+').ToArray());

    public static string? Tel(string? phone) => Dialable(phone) is { Length: > 0 } n ? "tel:" + n : null;

    public static string? Sms(string? phone) => Dialable(phone) is { Length: > 0 } n ? "sms:" + n : null;

    public static string? WhatsApp(string? phone) => International(phone) is { } n ? "https://wa.me/" + n : null;
}
