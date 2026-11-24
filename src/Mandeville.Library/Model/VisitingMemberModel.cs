namespace Mandeville.Library.Model;

/// <summary>A visiting member who has been here before (the "Been here before" quick picks).</summary>
public class VisitingMemberModel
{
    public Guid flngKey { get; set; }
    public string? fstrTitle { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string fstrBranch { get; set; } = string.Empty;
    public DateTime? fdtmLastVisit { get; set; }

    /// <summary>"Deacon Brian Clarke" (the title is left out when there is none).</summary>
    public string FullName => string.Join(' ', new[] { fstrTitle, fstrFirstname, fstrLastname }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
