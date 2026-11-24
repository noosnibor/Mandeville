using System.Net;
using System.Net.Mail;

namespace Mandeville.Web.Reports;

/// <summary>Emails a service report PDF using the SMTP account in the "Smtp" settings.</summary>
public class ReportMailer(IConfiguration configuration, ILogger<ReportMailer> logger)
{
    private readonly IConfiguration __configuration = configuration;
    private readonly ILogger<ReportMailer> __logger = logger;

    /// <summary>True when the mail account has been filled in.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(__configuration["Smtp:Host"]) && !string.IsNullOrWhiteSpace(__configuration["Smtp:From"]);

    /// <summary>The addresses suggested on the Reports page (Reports:Recipients, comma separated).</summary>
    public string DefaultRecipients => __configuration["Reports:Recipients"] ?? string.Empty;

    /// <summary>Splits and checks a list of email addresses. Invalid entries are returned in <paramref name="invalid"/>.</summary>
    public static List<string> ParseRecipients(string? text, out List<string> invalid)
    {
        invalid = [];
        var good = new List<string>();
        foreach (var part in (text ?? string.Empty).Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (MailAddress.TryCreate(part, out var address)) good.Add(address.Address);
            else invalid.Add(part);
        }
        return good.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Sends the PDF. Returns null on success, otherwise a message that is safe to show.</summary>
    public async Task<string?> SendAsync(ServiceReport report, byte[] pdf, IEnumerable<string> recipients, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Email is not set up yet. Add the Smtp settings first.";

        var to = recipients.ToList();
        if (to.Count == 0) return "Enter at least one email address.";

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

            using var message = new MailMessage { From = new MailAddress(from, report.GroupName) };
            foreach (var address in to) message.To.Add(address);

            message.Subject = $"Service report: {report.ServiceName}, {report.ServiceDate:d MMM yyyy}";
            message.Body =
                $"Good day,\n\nPlease find attached the service report for {report.ServiceName} on {report.ServiceDate:dddd d MMMM yyyy}.\n\n" +
                $"Visitors: {report.VisitorsAttended} ({report.WentToAltar} went to the altar)\n" +
                $"Members present: {report.MembersPresent} of {report.ActiveMembers} active ({report.PresentPercent}%)\n" +
                $"Visiting members: {report.VisitingMembers} (total in the service: {report.TotalInService})\n" +
                $"Collected this month so far: ${report.TotalCollected:N2}\n\n{report.GroupName}";

            message.Attachments.Add(new Attachment(new MemoryStream(pdf), report.FileName, "application/pdf"));

            await client.SendMailAsync(message, cancellationToken);
            return null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            __logger.LogError(ex, "Could not email the service report.");
            return "The email could not be sent. Check the Smtp settings. (" + ex.Message + ")";
        }
    }
}
