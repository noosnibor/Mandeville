namespace Mandeville.Library.Model;

/// <summary>A dated note kept for one visitor (versioned: flngVer = 0 is the live row).</summary>
public class VisitorNoteModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public Guid flngVisitorKey { get; set; }
    public DateTime fdtmNoteDate { get; set; }
    public string fstrNote { get; set; } = string.Empty;
    public bool fblnActive { get; set; } = true;
    public string? fstrWho { get; set; }
    public DateTime? fdtmWhen { get; set; }

    #endregion
}
