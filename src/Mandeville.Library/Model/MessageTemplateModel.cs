namespace Mandeville.Library.Model;

/// <summary>A saved piece of wording that people can edit, such as a visitor thank-you message.</summary>
public class MessageTemplateModel
{
    /// <summary>Which message this is. See <see cref="MessageTemplateCodes"/>.</summary>
    public string fstrCode { get; set; } = string.Empty;

    /// <summary>The wording, with {name}, {date}, {next} and {sender} filled in for each visitor.</summary>
    public string fstrText { get; set; } = string.Empty;

    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }
}

/// <summary>The codes of the messages that can be saved.</summary>
public static class MessageTemplateCodes
{
    public const string ThankFirst = "ThankFirst";
    public const string ThankSecond = "ThankSecond";
    public const string ThankThird = "ThankThird";

    /// <summary>The longest a saved message can be (the size of the database column).</summary>
    public const int MaxLength = 2000;
}
