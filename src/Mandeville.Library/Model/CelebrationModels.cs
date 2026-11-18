namespace Mandeville.Library.Model;

/// <summary>A card that was marked as sent (stored, so it does not come back in the same year).</summary>
public class CelebrationLogModel
{
    public Guid flngKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public string fstrType { get; set; } = string.Empty;
    public int flngYear { get; set; }
    public string fstrDisplayName { get; set; } = string.Empty;
    public string? fstrCaption { get; set; }
    /// <summary>How it went out: Email, WhatsApp or Manual.</summary>
    public string fstrChannel { get; set; } = "Manual";
    /// <summary>The address(es) the email went to.</summary>
    public string? fstrSentTo { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }
}

/// <summary>A card emailed later by the daily job (with the image and message chosen when it was queued).</summary>
public class CelebrationQueueModel
{
    public Guid flngKey { get; set; }
    public Guid flngMemberKey { get; set; }
    public Guid? flngSpouseMemberKey { get; set; }
    public string fstrType { get; set; } = string.Empty;
    public int flngYear { get; set; }
    /// <summary>The day it is celebrated; the email goes out on this day at the configured hour.</summary>
    public DateTime fdtmDueDate { get; set; }
    public string fstrDisplayName { get; set; } = string.Empty;
    public string fstrGreetingName { get; set; } = string.Empty;
    public string? fstrGender { get; set; }
    public int flngYears { get; set; }
    /// <summary>Comma separated addresses the email goes to.</summary>
    public string fstrEmails { get; set; } = string.Empty;
    /// <summary>Extra message written by the admin (shown under the greeting).</summary>
    public string? fstrMessage { get; set; }
    /// <summary>The uploaded card image; only filled when it was asked for.</summary>
    public byte[]? fbinImage { get; set; }
    public string? fstrImageContentType { get; set; }
    public bool fblnHasImage { get; set; }
    /// <summary>Queued, Sent, Failed or Cancelled.</summary>
    public string fstrStatus { get; set; } = "Queued";
    public string? fstrError { get; set; }
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    /// <summary>The card this queue row stands for, rebuilt from what was saved when it was queued.</summary>
    public CelebrationModel ToCelebration() => new()
    {
        Type = fstrType,
        MemberKey = flngMemberKey,
        SpouseKey = flngSpouseMemberKey,
        DisplayName = fstrDisplayName,
        ShortName = fstrGreetingName,
        GreetingName = fstrGreetingName,
        Gender = fstrGender ?? string.Empty,
        Years = flngYears,
        Emails = fstrEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
        Date = fdtmDueDate.Date,
        DaysAway = 0
    };
}

/// <summary>An upcoming birthday or anniversary (calculated, not stored).</summary>
public class CelebrationModel
{
    /// <summary>Birthday or Anniversary.</summary>
    public string Type { get; set; } = string.Empty;
    public bool IsBirthday => Type == "Birthday";

    /// <summary>The member the card is filed under (one of the two for a couple).</summary>
    public Guid MemberKey { get; set; }
    public Guid? SpouseKey { get; set; }

    /// <summary>Name shown on the card, for a couple e.g. "Grant and Joy Williams".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>First name used in the caption, for a couple the couple's names.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>The day it is celebrated this time round.</summary>
    public DateTime Date { get; set; }
    public int DaysAway { get; set; }

    /// <summary>Age being turned (birthdays) or years of marriage (anniversaries).</summary>
    public int Years { get; set; }

    /// <summary>Name(s) used in the greeting, e.g. "Marcus" or "Paul and Deifre".</summary>
    public string GreetingName { get; set; } = string.Empty;

    /// <summary>Male or Female for a birthday (decides the card picture); empty when not recorded or for anniversaries.</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>File name of the default card picture in wwwroot/cards (see <see cref="CardDesigns"/>).</summary>
    public string CardFile => CardDesigns.Default(this).File;

    /// <summary>Web address of the default card picture.</summary>
    public string CardImage => "/cards/" + CardFile;

    /// <summary>Valid email addresses the card can be emailed to (the member, and the spouse when they are a member).</summary>
    public List<string> Emails { get; set; } = [];

    public bool Sent { get; set; }
    public Guid? LogKey { get; set; }

    /// <summary>Set when Sent: Email, WhatsApp or Manual, the address(es), and when.</summary>
    public string? SentChannel { get; set; }
    public string? SentTo { get; set; }
    public DateTime? SentOn { get; set; }

    /// <summary>Set when an email with a chosen image / message is waiting for the daily job: Queued or Failed.</summary>
    public Guid? QueueKey { get; set; }
    public string? QueueStatus { get; set; }
    public string? QueueError { get; set; }

    /// <summary>Identifies this card for a given year, e.g. to remember a failed email.</summary>
    public string Id => $"{Type}:{MemberKey}:{Date.Year}";
}
