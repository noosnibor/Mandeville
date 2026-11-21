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

    public const string FailedMarker = "Thank-you WhatsApp message NOT sent";

    /// <summary>The note saved when a thank-you could not be sent, so the visitor is followed up another way.</summary>
    public static string FailedText(DateTime serviceDate) => $"{FailedMarker} for the service on {serviceDate:dd MMM yyyy}.";

    /// <summary>True when the note says the thank-you for that service date failed.</summary>
    public static bool IsFailedFor(string? note, DateTime serviceDate) =>
        note is not null && note.StartsWith(FailedMarker) && note.Contains(serviceDate.ToString("dd MMM yyyy"));

    /// <summary>The newest note (sent or failed) for that service date; a later note replaces an earlier one.</summary>
    public static VisitorNoteModel? Latest(IEnumerable<VisitorNoteModel> notes, DateTime serviceDate) =>
        notes.Where(n => IsFor(n.fstrNote, serviceDate) || IsFailedFor(n.fstrNote, serviceDate))
             .OrderByDescending(n => n.fdtmNoteDate).FirstOrDefault();
}
