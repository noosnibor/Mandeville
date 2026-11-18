using Microsoft.AspNetCore.Authorization;

namespace Mandeville.Web.Security;

/// <summary>
/// Put this on a page and it registers itself: the menu, the Roles &amp; access screen and the page's own security all pick it up.
/// Example: @attribute [PageAccess(PageKeys.Visitors, "Visitors", "People", "userplus", Tabs = "Add visitor,Returning visitor", Order = 20)]
/// A new page is closed to everyone but the Administrator until a role is given it on the Roles &amp; access screen.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PageAccessAttribute(string key, string name, string group, string icon) : Attribute, IAuthorizeData
{
    #region Properties

    /// <summary>Short, unchanging code for the page (stored against each role).</summary>
    public string Key { get; } = key;

    /// <summary>The name shown in the menu and on the Roles &amp; access screen.</summary>
    public string Name { get; } = name;

    /// <summary>The menu heading the page sits under (People, Finance...).</summary>
    public string Group { get; } = group;

    /// <summary>Icon name from the Icon component.</summary>
    public string Icon { get; } = icon;

    /// <summary>Menu icon colour (c1 to c6).</summary>
    public string Tint { get; set; } = "c2";

    /// <summary>Shorter name for the phone bottom bar.</summary>
    public string? Short { get; set; }

    /// <summary>Comma list of the page's tabs. Leave empty when the page has no tabs a role could be restricted to.</summary>
    public string Tabs { get; set; } = string.Empty;

    /// <summary>True when the page can show a person only their own records (a role can be set to "own only").</summary>
    public bool OwnOnlyOption { get; set; }

    /// <summary>True when only the Administrator may ever open the page (it cannot be given to a role).</summary>
    public bool AdminOnly { get; set; }

    /// <summary>True when the page may take a place on the phone's bottom bar.</summary>
    public bool Bar { get; set; }

    /// <summary>Position in the menu (lower first). Menu headings appear in the order of their first page.</summary>
    public int Order { get; set; } = 100;

    #endregion

    #region IAuthorizeData

    /// <summary>The page's own security rule, answered by <see cref="PageAccessPolicyProvider"/>.</summary>
    public string? Policy { get => PageAccessPolicy.For(Key); set { } }

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }

    #endregion
}
