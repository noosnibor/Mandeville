namespace Mandeville.Library.Model;

/// <summary>A person met on the street, kept so the tele team can call them.</summary>
public class EvangelismContactModel
{
    public Guid flngKey { get; set; }
    public string fstrFirstname { get; set; } = string.Empty;
    public string? fstrLastname { get; set; }
    public string? fstrPhoneNumber { get; set; }
    /// <summary>Where they live or where they were met.</summary>
    public string? fstrArea { get; set; }
    /// <summary>Prayer request or need.</summary>
    public string? fstrNeed { get; set; }
    public Guid? flngSessionKey { get; set; }
    public DateTime fdtmMet { get; set; } = AppClock.Today;
    public Guid? flngCallerKey { get; set; }
    public string fstrStatus { get; set; } = EvangelismRules.ContactNew;
    public int flngFailedAttempts { get; set; }
    public DateTime? fdtmLastAttempt { get; set; }
    /// <summary>For unreachable people: the day their personal details are removed.</summary>
    public DateTime? fdtmPurgeOn { get; set; }
    public bool fblnPurged { get; set; }
    public string? fstrNotes { get; set; }
    public bool fblnActive { get; set; } = true;

    /// <summary>The person agreed to be contacted. Calls should only be made when this is ticked.</summary>
    public bool fblnConsent { get; set; }
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    public string? fstrCallerName { get; set; }
    public int flngAttempts { get; set; }

    public string FullName => string.IsNullOrWhiteSpace(fstrLastname) ? fstrFirstname : $"{fstrFirstname} {fstrLastname}";

    /// <summary>A copy to edit without touching the row on screen.</summary>
    public EvangelismContactModel Copy() => (EvangelismContactModel)MemberwiseClone();
}

/// <summary>One call to a contact and how it went.</summary>
public class EvangelismAttemptModel
{
    public Guid flngKey { get; set; }
    public Guid flngContactKey { get; set; }
    public Guid? flngMemberKey { get; set; }
    public Guid? flngSessionKey { get; set; }
    public string fstrOutcome { get; set; } = EvangelismRules.Reached;
    public string? fstrNote { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }
    public string? fstrMemberName { get; set; }
}
