namespace Mandeville.Web.Components.Shared;

/// <summary>Neutral shape used by the Setup lookup editor for all five lookup tables.</summary>
public class LookupItem
{
    public Guid Key { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;

    /// <summary>Only used by payment types (marks a type as member dues).</summary>
    public bool IsDues { get; set; }
}
