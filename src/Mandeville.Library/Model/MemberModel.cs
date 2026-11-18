namespace Mandeville.Library.Model;

/// <summary>Member record (the live version, flngVer = 0).</summary>
public class MemberModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string fstrLastname { get; set; } = string.Empty;
    public string fstrGender { get; set; } = string.Empty;
    public string? fstrEmailAddress { get; set; }
    public string? fstrPhoneNumber { get; set; }
    public string? fstrHomeAddress { get; set; }
    public DateTime? fdtmDateOfBirth { get; set; }
    public DateTime? fdtmJoinDate { get; set; }
    public DateTime? fdtmLeaveGroupOn { get; set; }
    public Guid? flngMemberTypeKey { get; set; }
    public Guid? flngServiceAssignmentKey { get; set; }
    public string fstrMembershipStatus { get; set; } = string.Empty;
    public string? fstrNotes { get; set; }
    /// <summary>Core Member: may be given members to care for (Pastoral Care).</summary>
    public bool fblnCoreMember { get; set; }

    /// <summary>Single, Married, Divorced or Widowed.</summary>
    public string fstrMaritalStatus { get; set; } = "Single";
    /// <summary>The spouse when they are also a member of the group.</summary>
    public Guid? flngSpouseMemberKey { get; set; }
    /// <summary>The spouse's name when they are not a member of the group.</summary>
    public string? fstrSpouseName { get; set; }
    public DateTime? fdtmAnniversaryDate { get; set; }
    public string? fstrFormerSpouseName { get; set; }
    /// <summary>When true no birthday or anniversary card is suggested for this member.</summary>
    public bool fblnDontAnnounce { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion

    #region Read-only (joined / calculated)

    public string? fstrMemberTypeName { get; set; }
    public string? fstrServiceAssignmentName { get; set; }
    public string? fstrDepartments { get; set; }
    public string? fstrCarerName { get; set; }
    public Guid? flngCarerKey { get; set; }
    public string? fstrSpouseMemberName { get; set; }

    #endregion

    /// <summary>The spouse's name, from the linked member when there is one.</summary>
    public string? SpouseDisplayName => !string.IsNullOrWhiteSpace(fstrSpouseMemberName) ? fstrSpouseMemberName : fstrSpouseName;

    public string FullName => $"{fstrFirstname} {fstrLastname}";
}
