namespace Mandeville.Library.Model;

/// <summary>One team member's invite for one month: their private link and whether they have answered.</summary>
public class EvangelismInviteModel
{
    public Guid flngKey { get; set; }
    public Guid flngMemberKey { get; set; }
    /// <summary>First day of the month.</summary>
    public DateTime fdtmMonth { get; set; }
    public string fstrToken { get; set; } = string.Empty;
    public bool fblnNotAvailable { get; set; }
    public DateTime? fdtmSent { get; set; }
    public DateTime? fdtmResponded { get; set; }
    public string? fstrTitle { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string? fstrEmailAddress { get; set; }
    public string? fstrPhoneNumber { get; set; }
    /// <summary>How many Saturday / area choices the member ticked.</summary>
    public int flngChoices { get; set; }

    public bool Responded => fdtmResponded.HasValue;
    public string FullName => $"{fstrFirstname} {fstrLastname}";
    public string DisplayName => string.IsNullOrWhiteSpace(fstrTitle) ? FullName : $"{fstrTitle.Trim()} {FullName}";
}

/// <summary>One ticked box: the member can serve in this area on this Saturday.</summary>
public class EvangelismChoiceModel
{
    public Guid flngInviteKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public DateTime fdtmDate { get; set; }
    public string fstrArea { get; set; } = string.Empty;
}
