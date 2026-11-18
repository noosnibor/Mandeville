namespace Mandeville.Library.Model;

/// <summary>The wording of the visitor note that records a thank-you message (shared by the tab and the side panel).</summary>
public static class ThankYouNotes
{
    public const string Marker = "Thank-you WhatsApp message sent";

    /// <summary>The note text saved when a visitor is thanked for a service.</summary>
    public static string Text(DateTime serviceDate) => $"{Marker} for the service on {serviceDate:dd MMM yyyy}.";

    /// <summary>True when the note is a thank-you for that service date.</summary>
    public static bool IsFor(string? note, DateTime serviceDate) =>
        note is not null && note.StartsWith(Marker) && note.Contains(serviceDate.ToString("dd MMM yyyy"));
}
