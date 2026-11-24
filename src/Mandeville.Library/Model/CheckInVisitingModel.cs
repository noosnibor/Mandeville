namespace Mandeville.Library.Model;

/// <summary>Name and branch of a known visiting member, shown on the public check-in page.</summary>
public class CheckInVisitingModel
{
    public Guid flngVisitingMemberKey { get; set; }
    public string? fstrTitle { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string fstrBranch { get; set; } = string.Empty;
    public bool fblnAlreadyCheckedIn { get; set; }

    /// <summary>"Deacon Brian Clarke" (the title is left out when there is none).</summary>
    public string FullName => string.Join(' ', new[] { fstrTitle, fstrFirstname, fstrLastname }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
