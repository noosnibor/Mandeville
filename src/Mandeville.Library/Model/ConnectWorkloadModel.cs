namespace Mandeville.Library.Model;

/// <summary>A connection-team member and how many open visitors are assigned to them.</summary>
public class ConnectWorkloadModel
{
    public Guid flngMemberKey { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public int flngAssigned { get; set; }
    public string FullName => $"{fstrFirstname} {fstrLastname}";
}
