namespace Mandeville.Library.Model;

/// <summary>A member given to a Core Member to care for.</summary>
public class PastoralAssignmentModel
{
    public Guid flngKey { get; set; }
    public Guid flngCoreMemberKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public DateTime fdtmAssignedOn { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    public string? fstrCoreMemberName { get; set; }
    public string? fstrMemberName { get; set; }
}
