using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Mandeville.Library.Model;

namespace Mandeville.Web.Celebrations;

/// <summary>
/// Builds and sends the birthday and anniversary emails using the account in the "Smtp" settings.
/// Settings used: Smtp:*, Church:GroupName, Church:Ministers, Celebrations:AutoEmail, Celebrations:SendHour.
/// </summary>
public class CelebrationMailer(IConfiguration configuration, ILogger<CelebrationMailer> logger, IWebHostEnvironment environment)
{
    #region Private Fields

    private readonly IConfiguration __configuration = configuration;
    private readonly ILogger<CelebrationMailer> __logger = logger;
    private readonly IWebHostEnvironment __environment = environment;

    #endregion

    #region Settings

    /// <summary>True when the mail account has been filled in.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(__configuration["Smtp:Host"]) && !string.IsNullOrWhiteSpace(__configuration["Smtp:From"]);

    /// <summary>True when the daily automatic run is switched on (Celebrations:AutoEmail, on unless set to false).</summary>
    public bool AutoEmail => !string.Equals(__configuration["Celebrations:AutoEmail"], "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>Hour of the day (church time) the automatic run starts, 0 to 23. Default 8.</summary>
    public int SendHour => int.TryParse(__configuration["Celebrations:SendHour"], out var h) && h is >= 0 and <= 23 ? h : 8;

    public string GroupName => __configuration["Church:GroupName"] is { Length: > 0 } g ? g : "WAFIF Mandeville";

    private string Signature => __configuration["Church:Ministers"] is { Length: > 0 } m ? m : GroupName;

    #endregion

    #region Sending

    /// <summary>
    /// Emails the card to everyone in <see cref="CelebrationModel.Emails"/>, with the person's saved photo on the name plate when there is one.
    /// <paramref name="extraMessage"/> (the message shown under the card) replaces the standard paragraph, and <paramref name="customImage"/> replaces the standard card picture.
    /// Returns null on success, otherwise a message that is safe to show.
    /// </summary>
    public async Task<string?> SendAsync(CelebrationModel card, MemberPhotoModel? photo = null, string? extraMessage = null, byte[]? customImage = null, string? customImageType = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Email is not set up yet. Add the Smtp settings first.";
        if (card.Emails.Count == 0) return "There is no email address on record.";

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
            foreach (var address in card.Emails) message.To.Add(address);

            message.Subject = Subject(card);
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(PlainBody(card, extraMessage), System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain));

            // The card picture travels inside the email (not a link), so it shows without "load images"
            var picture = Path.Combine(__environment.WebRootPath ?? string.Empty, "cards", card.CardFile);
            var hasCustom = customImage is { Length: > 0 };
            var hasPicture = hasCustom || File.Exists(picture);
            var htmlView = AlternateView.CreateAlternateViewFromString(HtmlBody(card, hasPicture, photo is not null, extraMessage), System.Text.Encoding.UTF8, MediaTypeNames.Text.Html);
            if (hasCustom) htmlView.LinkedResources.Add(new LinkedResource(new MemoryStream(customImage!), string.IsNullOrWhiteSpace(customImageType) ? "image/jpeg" : customImageType) { ContentId = "cardpicture", TransferEncoding = TransferEncoding.Base64 });
            else if (hasPicture) htmlView.LinkedResources.Add(new LinkedResource(picture, "image/jpeg") { ContentId = "cardpicture", TransferEncoding = TransferEncoding.Base64 });
            if (photo is not null && hasPicture)
                htmlView.LinkedResources.Add(new LinkedResource(new MemoryStream(photo.fbinPhoto), photo.fstrContentType) { ContentId = "memberphoto", TransferEncoding = TransferEncoding.Base64 });
            message.AlternateViews.Add(htmlView);

            await client.SendMailAsync(message, cancellationToken);
            return null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            __logger.LogError(ex, "Could not email the {Type} card for {Name}.", card.Type, card.DisplayName);
            return "The email could not be sent. Check the Smtp settings. (" + ex.Message + ")";
        }
    }

    #endregion

    #region Message text

    /// <summary>The top of the email: the card picture with a name plate, or the coloured banner when the picture is missing.</summary>
    private string HeaderBlock(CelebrationModel c, bool hasPicture, bool showPhoto, string colour, string gradient, string emoji, string detail, string? extra = null)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        // The person's photo sits on the name plate (round in most mail apps, square in some older ones)
        var avatar = showPhoto
            ? "<img src=\"cid:memberphoto\" width=\"110\" height=\"110\" alt=\"\" style=\"display:block;margin:0 auto 12px;width:110px;height:110px;border-radius:55px;border:4px solid #ffffff;object-fit:cover;\">"
            : string.Empty;

        // With the admin's message, the plate under the picture carries the message in place of the name
        var plate = string.IsNullOrWhiteSpace(extra)
            ? $"""
      <div style="font-family:Georgia,'Times New Roman',serif;font-size:24px;font-weight:700;letter-spacing:1px;color:#7a1c3a;text-transform:uppercase;">{E(c.DisplayName)}</div>
      <div style="font-family:Georgia,'Times New Roman',serif;font-size:16px;font-style:italic;color:#5b4a3a;margin-top:4px;">{E(detail)}</div>
      <div style="font-size:11px;letter-spacing:3px;margin-top:10px;color:#a67c2e;font-weight:600;">{E(GroupName.ToUpperInvariant())}</div>
"""
            : $"""
      <div style="font-family:Georgia,'Times New Roman',serif;font-size:16px;line-height:1.6;color:#5b4a3a;text-align:left;">{E(extra.Trim()).Replace("\r\n", "\n").Replace("\n", "<br>")}</div>
""";

        if (hasPicture)
            return $$"""
    <tr><td style="background:#ffffff;border-radius:20px 20px 0 0;overflow:hidden;line-height:0;"><img src="cid:cardpicture" width="560" alt="{{E(Heading(c))}}" style="display:block;width:100%;height:auto;border:0;border-radius:20px 20px 0 0;"></td></tr>
    <tr><td align="center" style="background:#fbf6f0;border-top:3px solid #c9a24d;border-radius:0 0 20px 20px;padding:18px 20px;">
      {{avatar}}
{{plate}}
    </td></tr>
""";

        return $$"""
    <tr><td align="center" style="background:{{colour}};background-image:{{gradient}};border-radius:20px;padding:34px 24px;color:#ffffff;">
      <div style="font-size:48px;line-height:56px;">{{emoji}}</div>
      <div style="font-size:12px;letter-spacing:2px;margin-top:6px;opacity:.9;">{{E(Heading(c))}}</div>
      <div style="font-size:28px;font-weight:800;margin:10px 0 6px;">{{E(c.DisplayName)}}</div>
      <div style="font-size:14px;opacity:.95;">{{E(detail)}}</div>
      <div style="font-size:11px;letter-spacing:3px;margin-top:18px;opacity:.85;">{{E(GroupName.ToUpperInvariant())}}</div>
    </td></tr>
""";
    }


    private static string Ordinal(int n)
    {
        var suffix = (n % 100) is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    public string Subject(CelebrationModel c) => c.IsBirthday
        ? $"Happy Birthday, {c.GreetingName}! \U0001F389"
        : $"Happy {(c.Years < 1 ? "" : Ordinal(c.Years) + " ")}anniversary, {c.GreetingName}! \U0001F48D";

    private string Heading(CelebrationModel c) => c.IsBirthday
        ? "HAPPY BIRTHDAY"
        : c.Years < 1 ? "HAPPY ANNIVERSARY" : $"HAPPY {Ordinal(c.Years).ToUpperInvariant()} ANNIVERSARY";

    private string Message(CelebrationModel c) => c.IsBirthday
        ? $"Wishing you a wonderful day and a blessed year ahead. We are grateful to have you in our church family."
        : "Celebrating the love and faith you share. We thank God for your marriage and pray for many more years together.";

    private string PlainBody(CelebrationModel c, string? extra = null) =>
        $"{(string.IsNullOrWhiteSpace(extra) ? $"Dear {c.GreetingName},\r\n\r\n{Message(c)}" : extra.Trim())}\r\n\r\nWith love,\r\n{Signature}\r\nand everyone at {GroupName}\r\n";

    /// <summary>A table based email so it looks the same in Gmail, Outlook and phone mail apps, with no images to load.</summary>
    private string HtmlBody(CelebrationModel c, bool hasPicture, bool hasMemberPhoto, string? extra = null)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        // The admin's own words, with line breaks kept
        // The admin's message (as written for WhatsApp) replaces the standard paragraph; without one the standard greeting is used
        var bodyHtml = string.IsNullOrWhiteSpace(extra)
            ? $"Dear {E(c.GreetingName)},<br><br>{E(Message(c))}<br><br>"
            : hasPicture ? string.Empty : E(extra.Trim()).Replace("\r\n", "\n").Replace("\n", "<br>") + "<br><br>";

        var colour = c.IsBirthday ? "#0f766e" : "#be123c";
        var gradient = c.IsBirthday ? "linear-gradient(135deg,#0f766e,#14b8a6 60%,#5eead4)" : "linear-gradient(135deg,#9f1239,#e11d48 55%,#fb7185)";
        var emoji = c.IsBirthday ? "\U0001F382" : "\U0001F48D";
        var detail = c.IsBirthday ? c.Date.ToString("dddd d MMMM") : c.Years < 1 ? c.Date.ToString("dddd d MMMM") : $"{c.Years} year{(c.Years == 1 ? "" : "s")} together";

        return $$"""
<!DOCTYPE html>
<html><body style="margin:0;padding:0;background:#f3f4f8;font-family:Segoe UI,Arial,sans-serif;">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f8;padding:24px 12px;"><tr><td align="center">
  <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
{{HeaderBlock(c, hasPicture, hasPicture && hasMemberPhoto, colour, gradient, emoji, detail, extra)}}
    <tr><td style="height:14px;line-height:14px;">&nbsp;</td></tr>
    <tr><td style="background:#ffffff;border-radius:16px;padding:20px 22px;color:#334155;font-size:15px;line-height:1.6;">
      {{bodyHtml}}With love,<br>{{E(Signature)}}<br>and everyone at {{E(GroupName)}}
    </td></tr>
    <tr><td align="center" style="padding-top:12px;color:#94a3b8;font-size:12px;">You are receiving this because you are a member of {{E(GroupName)}}.</td></tr>
  </table>
</td></tr></table>
</body></html>
""";
    }

    #endregion
}
