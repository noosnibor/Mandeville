using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mandeville.Web.Reports;

/// <summary>Draws a <see cref="PeriodReport"/> as an A4 PDF.</summary>
public static class PeriodReportPdf
{
    private const string Blue = "#1670d6";
    private const string Ink = "#12151c";
    private const string Muted = "#667085";
    private const string Line = "#e4e7ec";
    private const string Soft = "#f4f8ff";

    /// <summary>Returns the PDF bytes.</summary>
    public static byte[] Create(PeriodReport r)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#344054"));

                page.Header().PaddingBottom(10).BorderBottom(2).BorderColor(Blue).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"{r.Kind} Report").FontSize(22).SemiBold().FontColor(Ink);
                        c.Item().Text($"{r.GroupName}  |  {r.Title}").FontColor(Muted);
                        c.Item().Text(r.RangeText).FontColor(Muted).FontSize(9);
                    });
                    row.ConstantItem(150).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("Prepared for the Minister").FontColor(Muted).FontSize(9);
                        c.Item().AlignRight().Text($"Generated {r.GeneratedOn:d MMM yyyy}").FontColor(Muted).FontSize(9);
                    });
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(6);

                    // 1. Attendance
                    col.Item().Element(c => Heading(c, "1. Attendance"));
                    col.Item().Row(row =>
                    {
                        Tile(row, r.Services.ToString(), "Services held");
                        Tile(row, r.AvgMembers.ToString(), r.PeakMembers > 0 ? $"Average members (peak {r.PeakMembers})" : "Average members");
                        Tile(row, r.AvgVisitors.ToString(), "Average visitors");
                        Tile(row, r.AvgTotal.ToString(), "Average in the service");
                    });

                    if (r.Rows.Count == 0)
                        col.Item().Text("No services were held in this period.").FontColor(Muted);
                    else
                        col.Item().Element(c => AttendanceTable(c, r));

                    // 2. Visitors
                    col.Item().PaddingTop(8).Element(c => Heading(c, "2. Visitors"));
                    col.Item().Row(row =>
                    {
                        Tile(row, r.VisitorVisits.ToString(), "Visitor visits");
                        Tile(row, r.DifferentVisitors.ToString(), "Different visitors");
                        Tile(row, r.FirstTimeVisitors.ToString(), "First-time visitors");
                        Tile(row, r.WentToAltar.ToString(), "Went to the altar");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Element(x => Header(x, "Visitors", "Total"));
                            c.Item().Element(x => Line_(x, "Male / Female", $"{r.VisitorsMale} / {r.VisitorsFemale}"));
                            c.Item().Element(x => Line_(x, "Adult / Child", $"{r.VisitorsAdult} / {r.VisitorsChild}"));
                            c.Item().Element(x => Line_(x, "Baptised in the period", r.Baptised.ToString()));
                        });
                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Element(x => Header(x, "Reason for the altar", "Total"));
                            foreach (var (reason, count) in r.AltarReasons) c.Item().Element(x => Line_(x, reason, count.ToString()));
                        });
                    });

                    // 3. Membership growth
                    col.Item().PaddingTop(8).Element(c => Heading(c, "3. Membership growth"));
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Element(x => Line_(x, "Started Tier 1", r.Tier1.ToString()));
                            c.Item().Element(x => Line_(x, "Completed Tier 2", r.Tier2.ToString()));
                            c.Item().Element(x => Line_(x, "Interviewed", r.Interviews.ToString()));
                        });
                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Element(x => Line_(x, "Inaugurated", r.Inaugurated.ToString()));
                            c.Item().Element(x => Line_(x, "Baptised", r.Baptised.ToString()));
                            c.Item().Element(x => Line_(x, "Became members", r.MembersJoined.ToString()));
                        });
                    });

                    // 4. Members and care
                    col.Item().PaddingTop(8).Element(c => Heading(c, "4. Members and pastoral care"));
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Element(x => Line_(x, "Different members present", r.DifferentMembersPresent.ToString()));
                            c.Item().Element(x => Line_(x, "Care contacts made", r.CareContacts.ToString()));
                            c.Item().Element(x => Line_(x, "Members contacted", r.MembersContacted.ToString()));
                        });
                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Element(x => Line_(x, "Flagged for the Minister", r.FlagsRaised.ToString()));
                            c.Item().Element(x => Line_(x, "Visiting members (all branches)", r.DifferentVisitingMembers.ToString()));
                            foreach (var b in r.VisitingByBranch)
                                c.Item().Element(x => Line_(x, $"Visiting from {b.Key}", b.Value.ToString()));
                        });
                    });

                    // 5. Evangelism
                    var financeNo = r.HasEvangelism ? 6 : 5;
                    if (r.HasEvangelism)
                    {
                        var ev = r.Evangelism;
                        col.Item().PaddingTop(8).Element(c => Heading(c, "5. Evangelism"));
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(12).Column(c =>
                            {
                                c.Item().Element(x => Line_(x, "Sessions held", ev.Sessions.ToString(), bold: true));
                                c.Item().Element(x => Line_(x, "Present of scheduled", $"{ev.Present} of {ev.Scheduled} ({ev.TurnUpPercent}%)"));
                                c.Item().Element(x => Line_(x, "Flyers handed out", ev.Flyers.ToString()));
                            });
                            row.RelativeItem().PaddingLeft(12).Column(c =>
                            {
                                c.Item().Element(x => Line_(x, "Persons ministered to", ev.Ministered.ToString()));
                                c.Item().Element(x => Line_(x, "New contacts", ev.NewContacts.ToString()));
                                c.Item().Element(x => Line_(x, "Calls made / reached", $"{ev.Calls} / {ev.Reached}"));
                            });
                        });
                    }

                    // Finance
                    col.Item().PaddingTop(8).Element(c => Heading(c, $"{financeNo}. Financial overview"));
                    col.Item().Element(x => Line_(x, $"Dues collected ({r.CollectionsLabel})", Money(r.DuesCollected)));
                    col.Item().Element(x => Line_(x, $"Other collections ({r.CollectionsLabel})", Money(r.OtherCollected)));
                    col.Item().Element(x => Line_(x, "Total collected", Money(r.TotalCollected), bold: true));
                    col.Item().Element(x => Line_(x, $"Expenses ({r.From:d MMM} to {r.To:d MMM})", Money(r.Expenses), bold: true));
                    if (r.Net is { } net)
                        col.Item().Element(x => Line_(x, "Collected less expenses", Money(net), bold: true));
                    if (r.ExpenseCategories.Count > 0)
                        col.Item().Element(x => Line_(x, "Largest expense categories", string.Join("  |  ", r.ExpenseCategories.Take(3).Select(x2 => $"{x2.Category} {Money(x2.Amount)}"))));

                    col.Item().PaddingTop(6).Text(r.CollectionsAreContext
                            ? "Collections are recorded by month. This period is shorter than a month, so the figures above are the month so far and are not added to the expenses."
                            : "Collections are recorded by month, so only months that fall completely inside the period are included. Expenses are exact for the dates shown.")
                        .FontSize(8).FontColor(Muted);
                });

                page.Footer().BorderTop(1).BorderColor(Line).PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"{r.GroupName}  |  Confidential: for the Minister").FontSize(8).FontColor(Muted);
                    row.ConstantItem(80).AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    #region Building blocks

    private static string Money(decimal value) => "$" + value.ToString("N2");

    private static void AttendanceTable(IContainer c, PeriodReport r) =>
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                for (var i = 0; i < 5; i++) cols.RelativeColumn(1);
            });

            table.Header(h =>
            {
                h.Cell().Element(x => HeadCell(x, r.RowsAreMonths ? "Month (totals)" : "Service", true));
                foreach (var t in new[] { "Members", "Visitors", "Visiting", "Total", "Altar" }) h.Cell().Element(x => HeadCell(x, t, false));
            });

            foreach (var row in r.Rows)
            {
                table.Cell().Element(x => LabelCell(x, row));
                foreach (var v in new[] { row.Members, row.Visitors, row.Visiting, row.Total, row.Altar })
                    table.Cell().Element(x => BodyCell(x, v.ToString()));
            }

            table.Cell().Element(x => FootCell(x, "Total", true));
            foreach (var v in new[] { r.MemberCheckIns, r.VisitorVisits, r.VisitingCheckIns, r.TotalAttendance, r.AltarTotal })
                table.Cell().Element(x => FootCell(x, v.ToString(), false));

            table.Cell().Element(x => FootCell(x, "Average per service", true));
            foreach (var v in new[] { r.AvgMembers, r.AvgVisitors, r.AvgVisiting, r.AvgTotal })
                table.Cell().Element(x => FootCell(x, v.ToString(), false));
            table.Cell().Element(x => FootCell(x, "", false));
        });

    private static void HeadCell(IContainer c, string text, bool left)
    {
        var cell = c.BorderBottom(1).BorderColor(Line).PaddingVertical(3);
        (left ? cell.AlignLeft() : cell.AlignRight()).Text(text).FontSize(8).FontColor(Muted);
    }

    private static void LabelCell(IContainer c, PeriodRow row) =>
        c.BorderBottom(0.5f).BorderColor(Line).PaddingVertical(4).Column(cc =>
        {
            cc.Item().Text(row.Label).SemiBold().FontColor(Ink);
            cc.Item().Text(row.Sub).FontSize(8).FontColor(Muted);
        });

    private static void BodyCell(IContainer c, string text) =>
        c.BorderBottom(0.5f).BorderColor(Line).PaddingVertical(4).AlignRight().Text(text);

    private static void FootCell(IContainer c, string text, bool left)
    {
        var cell = c.Background(Soft).PaddingVertical(4).PaddingHorizontal(2);
        (left ? cell.AlignLeft() : cell.AlignRight()).Text(text).SemiBold().FontColor(Ink);
    }

    private static void Heading(IContainer c, string text) =>
        c.PaddingTop(4).Text(text.ToUpperInvariant()).FontSize(9).Bold().FontColor(Blue).LetterSpacing(0.08f);

    private static void Tile(RowDescriptor row, string value, string label) =>
        row.RelativeItem().Padding(3).Border(1).BorderColor(Line).Padding(8).Column(c =>
        {
            c.Item().Text(value).FontSize(17).SemiBold().FontColor(Ink);
            c.Item().Text(label).FontSize(8).FontColor(Muted);
        });

    private static void Header(IContainer c, string left, string right) =>
        c.BorderBottom(1).BorderColor(Line).PaddingVertical(3).Row(row =>
        {
            row.RelativeItem().Text(left).FontSize(8).FontColor(Muted);
            row.ConstantItem(90).AlignRight().Text(right).FontSize(8).FontColor(Muted);
        });

    private static void Line_(IContainer c, string label, string value, bool bold = false) =>
        c.BorderBottom(0.5f).BorderColor(Line).PaddingVertical(4).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(170).AlignRight().Text(value).FontColor(Ink).Bold();
        });

    #endregion
}
