using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Mandeville.Web.Security;

/// <summary>Everything a page declares about itself with [PageAccess].</summary>
public sealed record PageInfo(string Key, string Name, string Group, string Icon, string Tint, string Short, string Href,
    string[] Tabs, bool OwnOnlyOption, bool AdminOnly, bool Bar, int Order);

/// <summary>The keys of the pages, so a typing slip is a compile error instead of a page nobody can open.</summary>
public static class PageKeys
{
    public const string Dashboard = "dashboard";
    public const string Members = "members";
    public const string Visitors = "visitors";
    public const string Connect = "connect";
    public const string Incoming = "incoming";
    public const string Pastoral = "pastoral";
    public const string Collections = "collections";
    public const string Expenditure = "expenditure";
    public const string Ledger = "ledger";
    public const string Evangelism = "evangelism";
    public const string Reports = "reports";
    public const string Setup = "setup";
    public const string Users = "users";
}

/// <summary>Finds every page that carries [PageAccess] (once, when first asked) so nothing has to be listed by hand.</summary>
public static class PageRegistry
{
    #region Private Fields

    private static readonly Lazy<IReadOnlyList<PageInfo>> __all = new(Build);

    #endregion

    #region Lookups

    /// <summary>Every registered page, in menu order.</summary>
    public static IReadOnlyList<PageInfo> All => __all.Value;

    public static PageInfo? Find(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Splits a comma list of tab names.</summary>
    public static string[] SplitTabs(string? tabs) =>
        string.IsNullOrWhiteSpace(tabs) ? [] : tabs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    #endregion

    #region Scan

    private static IReadOnlyList<PageInfo> Build()
    {
        var pages = new List<PageInfo>();

        foreach (var type in typeof(PageRegistry).Assembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<PageAccessAttribute>(inherit: false);
            if (attribute is null) continue;

            // The first @page route of the component is the page's address ("/" becomes an empty href)
            var route = type.GetCustomAttributes<RouteAttribute>(inherit: false).Select(r => r.Template).FirstOrDefault() ?? "/";

            pages.Add(new PageInfo(
                attribute.Key, attribute.Name, attribute.Group, attribute.Icon, attribute.Tint,
                string.IsNullOrWhiteSpace(attribute.Short) ? attribute.Name : attribute.Short,
                route.Trim('/'), SplitTabs(attribute.Tabs), attribute.OwnOnlyOption, attribute.AdminOnly, attribute.Bar, attribute.Order));
        }

        return [.. pages.OrderBy(p => p.Order).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)];
    }

    #endregion
}
