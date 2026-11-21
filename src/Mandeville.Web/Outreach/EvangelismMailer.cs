using System.Net;
using System.Net.Mail;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;
using Microsoft.AspNetCore.Components;

namespace Mandeville.Web.Outreach;

/// <summary>Finds the evangelism team: the department with "evangel" in its name, or every active member when there is none.</summary>
public static class OutreachTeam
{
    public static async Task<Guid?> DepartmentKeyAsync(IDepartmentRepository repo, CancellationToken cancellationToken)
    {
        var departments = await repo.SelectDepartment(active: true, cancellationToken: cancellationToken);
        return departments.FirstOrDefault(d => d.fstrName.Contains("evangel", StringComparison.OrdinalIgnoreCase))?.flngKey;
    }
}

/// <summary>
/// Writes the invite and schedule messages for the evangelism team and sends them by email with the "Smtp" settings.
/// The same text is used for the WhatsApp and mail-app links, so the team lead can send it by hand when email is not set up.
/// </summary>
public class EvangelismMailer(IConfiguration configuration, ILogger<EvangelismMailer> logger, NavigationManager navigation)
{
    #region Private Fields

    private readonly IConfiguration __configuration = configuration;
    private readonly ILogger<EvangelismMailer> __logger = logger;
    private readonly NavigationManager __navigation = navigation;

    #endregion

    #region Settings

    /// <summary>True when the mail account has been filled in.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(__configuration["Smtp:Host"]) && !string.IsNullOrWhiteSpace(__configuration["Smtp:From"]);

    public string GroupName => __configuration["Church:GroupName"] is { Length: > 0 } g ? g : "WAFIF Mandeville";

    /// <summary>The web address of the site, with a closing slash (Church:PublicBaseUrl, otherwise the address being used now).</summary>
    public string BaseUrl
    {
        get
        {
            var b = __configuration["Church:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(b)) b = __navigation.BaseUri;
            return b.TrimEnd('/') + "/";
        }
    }

    /// <summary>The member's private page for answering and confirming.</summary>
    public string AvailabilityLink(string token) => $"{BaseUrl}evangelism/availability/{token}";

    #endregion

    #region Message text

    /// <summary>Asks the member which Saturdays and areas they can serve.</summary>
    public (string Subject, string Body) InviteMessage(EvangelismInviteModel invite)
    {
        var month = invite.fdtmMonth.ToString("MMMM yyyy");
        var subject = $"Evangelism team: your availability for {month}";
        var body =
            $"Hi {invite.fstrFirstname},\n\n" +
            $"We are putting together the evangelism schedule for {month}. Please tell us which Saturdays and areas you can serve " +
            $"(Street Evangelism, Tele Evangelism or Intercessory Prayer). You will be scheduled at least once and at most twice a month, " +
            $"and you can also say you are not available this month.\n\n" +
            $"{AvailabilityLink(invite.fstrToken)}\n\n" +
            $"Thank you,\n{GroupName} Evangelism team";
        return (subject, body);
    }

    /// <summary>Tells the member where they are scheduled and asks them to confirm.</summary>
    public (string Subject, string Body) ScheduleMessage(EvangelismInviteModel invite, IEnumerable<EvangelismAssignmentModel> mine)
    {
        var month = invite.fdtmMonth.ToString("MMMM yyyy");
        var lines = mine.Where(a => a.fstrStatus != EvangelismRules.Declined)
            .OrderBy(a => a.fdtmSessionDate)
            .Select(a =>
            {
                var time = a.ftimSessionTime.HasValue ? ", " + DateTime.Today.Add(a.ftimSessionTime.Value).ToString("h:mm tt") : string.Empty;
                var place = string.IsNullOrWhiteSpace(a.fstrSessionLocation) ? string.Empty : ", " + a.fstrSessionLocation;
                var lead = a.fstrRole == "Team lead" ? " (you are the team lead)" : string.Empty;
                return $"- {a.fdtmSessionDate:dddd d MMMM}{time}: {a.fstrSessionArea}{place}{lead}";
            })
            .ToList();

        var subject = $"Evangelism team: your schedule for {month}";
        var body =
            $"Hi {invite.fstrFirstname},\n\n" +
            $"Thank you for making yourself available. Here is where you are scheduled in {month}:\n\n" +
            string.Join("\n", lines) + "\n\n" +
            $"Please confirm, or tell us if you can no longer make it:\n{AvailabilityLink(invite.fstrToken)}\n\n" +
            $"Thank you,\n{GroupName} Evangelism team";
        return (subject, body);
    }

    #endregion

    #region Sending

    /// <summary>Sends a plain-text email. Returns null on success, otherwise a message that is safe to show.</summary>
    public async Task<string?> SendAsync(string? toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Email is not set up yet. Add the Smtp settings first.";
        if (!MailAddress.TryCreate(toEmail, out var to)) return "There is no valid email address.";

        try
        {
            var host = __configuration["Smtp:Host"]!;
            var port = int.TryParse(__configuration["Smtp:Port"], out var p) ? p : 587;
            var from = __configuration["Smtp:From"]!;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = !string.Equals(__configuration["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase)
            };
            var user = __configuration["Smtp:User"];
            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, __configuration["Smtp:Password"]);

            using var message = new MailMessage { From = new MailAddress(from, GroupName) };
            message.To.Add(to);
            message.Subject = subject;
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.Body = body;

            await client.SendMailAsync(message, cancellationToken);
            return null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            __logger.LogError(ex, "Could not email the evangelism message to {Address}.", toEmail);
            return "The email could not be sent. Check the Smtp settings. (" + ex.Message + ")";
        }
    }

    #endregion
}
