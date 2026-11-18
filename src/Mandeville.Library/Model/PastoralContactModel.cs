namespace Mandeville.Library.Model;

/// <summary>One logged contact (call, visit ...) between a Core Member and a member in their care.</summary>
public class PastoralContactModel
{
    public Guid flngKey { get; set; }
    public Guid flngCoreMemberKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public DateTime fdtmContactDate { get; set; }
    public string fstrMethod { get; set; } = "Phone call";
    public string? fstrNotes { get; set; }
    public DateTime? fdtmNextContactDate { get; set; }
    public bool fblnFlagForMinister { get; set; }
    public bool fblnFlagResolved { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    public string? fstrCoreMemberName { get; set; }
    public string? fstrMemberName { get; set; }
}
