namespace Mandeville.Library.Model;

/// <summary>Name-only member result shown on the public check-in page.</summary>
public class CheckInMemberModel
{
    public Guid flngMemberKey { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public bool fblnAlreadyCheckedIn { get; set; }
    public string FullName => $"{fstrFirstname} {fstrLastname}";
}
