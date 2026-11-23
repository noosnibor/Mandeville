using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mandeville.Web.Reports;

/// <summary>Draws a <see cref="ServiceReport"/> as a one-page A4 PDF.</summary>
public static class ServiceReportPdf
{
    private const string Teal = "#1670d6";
    private const string Ink = "#12151c";
    private const string Muted = "#667085";
    private const string Line = "#e4e7ec";

    /// <summary>Returns the PDF bytes.</summary>
    public static byte[] Create(ServiceReport r)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#344054"));

                #region Header

                page.Header().PaddingBottom(10).BorderBottom(2).BorderColor(Teal).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Service Report").FontSize(22).SemiBold().FontColor(Ink);
                        c.Item().Text($"{r.GroupName}  |  {r.ServiceName}, {r.ServiceDate:dddd d MMMM yyyy}").FontColor(Muted);
                    });
                    row.ConstantItem(150).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("Prepared for the Minister").FontColor(Muted).FontSize(9);
                        c.Item().AlignRight().Text($"Generated {r.GeneratedOn:d MMM yyyy}").FontColor(Muted).FontSize(9);
                    });
                });

                #endregion

                #region Content

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(6);

                    // 1. Visitors
                    col.Item().Element(c => Heading(c, "1. Visitors"));
                    col.Item().Row(row =>
                    {
                        Tile(row, r.VisitorsAttended.ToString(), "Visitors attended");
                        Tile(row, $"{r.VisitorsMale} / {r.VisitorsFemale}", "Male / Female");
                        Tile(row, $"{r.VisitorsAdult} / {r.VisitorsChild}", "Adult / Child");
                        Tile(row, r.WentToAltar.ToString(), "Went to the altar");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Element(x => Header(x, "Reason for the altar", "Total"));
                            foreach (var (reason, count) in r.AltarReasons) c.Item().Element(x => Line_(x, reason, count.ToString()));
                        });
                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Element(x => Header(x, "Follow-up", "Total"));
                            c.Item().Element(x => Line_(x, "Baptised", r.Baptised.ToString()));
                            c.Item().Element(x => Line_(x, "Called / contacted", $"{r.Called} of {r.VisitorsAttended}"));
                            c.Item().Element(x => Line_(x, "Started membership class", r.StartedClass.ToString()));
                            c.Item().Element(x => Line_(x, "Waiting for a connector", r.WaitingForConnector.ToString()));
                        });
                    });

                    // 2. Incoming members
                    col.Item().PaddingTop(8).Element(c => Heading(c, "2. Incoming members"));
                    col.Item().Row(row =>
                    {
                        Tile(row, r.IncomingTotal.ToString(), "Incoming members");
                        Tile(row, r.IncomingNewBelievers.ToString(), "New believers");
                        Tile(row, $"{r.IncomingReturning} / {r.IncomingJoining}", "Returning / Joining");
                        Tile(row, r.IncomingPartners.ToString(), "Partners");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Element(x => Header(x, "Membership process (partners excluded)", "Done"));
                            c.Item().Element(x => Line_(x, "Tier 1", r.Tier1Done.ToString()));
                            c.Item().Element(x => Line_(x, "Tier 2", r.Tier2Done.ToString()));
                            c.Item().Element(x => Line_(x, "Interview", r.InterviewDone.ToString()));
                            c.Item().Element(x => Line_(x, "Inauguration", r.InauguratedDone.ToString()));
                        });
                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Element(x => Header(x, "This week", "Total"));
                            c.Item().Element(x => Line_(x, "Completed Tier 1 / Tier 2", $"{r.WeekTier1} / {r.WeekTier2}"));
                            c.Item().Element(x => Line_(x, "Interviewed / Inaugurated", $"{r.WeekInterview} / {r.WeekInaugurated}"));
                            c.Item().Element(x => Line_(x, "Baptised", r.WeekBaptised.ToString()));
                            c.Item().Element(x => Line_(x, "Ready to convert to member", r.ReadyToConvert.ToString()));
                            c.Item().Element(x => Line_(x, "Waiting for a connector", r.IncomingWaitingForConnector.ToString()));
                        });
                    });

                    // 3. Members and pastoral care
                    col.Item().PaddingTop(8).Element(c => Heading(c, "3. Members and pastoral care"));
                    col.Item().Row(row =>
                    {
                        Tile(row, r.TotalMembers.ToString(), "Total members");
                        Tile(row, r.ActiveMembers.ToString(), "Active");
                        Tile(row, r.InactiveMembers.ToString(), "Inactive");
                        Tile(row, $"{r.MembersPresent} ({r.PresentPercent}%)", "Present at this service");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(12).Column(c =>
                        {
                            c.Item().Element(x => Line_(x, "Persons connected to a carer", $"{r.Connected} of {r.Careable}"));
                            c.Item().Element(x => Line_(x, "Core members caring", r.CoreMembers.ToString()));
                            c.Item().Element(x => Line_(x, "Care contacts this week", r.CareContactsThisWeek.ToString()));
                        });
                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Element(x => Line_(x, "Not seen in 3+ weeks", r.NotSeen3Weeks.ToString()));
                            c.Item().Element(x => Line_(x, "Overdue for a care contact", r.OverdueForContact.ToString()));
                            c.Item().Element(x => Line_(x, "Flagged for the Minister", r.FlaggedForMinister.ToString()));
                        });
                    });

                    if (r.MembersByTitle.Count > 0)
                        col.Item().PaddingTop(4).Element(x => Line_(x, "Active members by title", string.Join("  |  ", r.MembersByTitle.Select(t => $"{t.Title} {t.Count}"))));

                    // 4. Evangelism (numbers only)
                    if (r.HasEvangelism)
                    {
                        col.Item().PaddingTop(8).Element(c => Heading(c, "4. Evangelism"));
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(12).Column(c =>
                            {
                                c.Item().Element(x => Line_(x, "Last Saturday: sessions held", r.EvangelismSaturday.Sessions.ToString(), bold: true));
                                c.Item().Element(x => Line_(x, "Present of scheduled", $"{r.EvangelismSaturday.Present} of {r.EvangelismSaturday.Scheduled} ({r.EvangelismSaturday.TurnUpPercent}%)"));
                                c.Item().Element(x => Line_(x, "Flyers handed out", r.EvangelismSaturday.Flyers.ToString()));
                                c.Item().Element(x => Line_(x, "Persons ministered to", r.EvangelismSaturday.Ministered.ToString()));
                                c.Item().Element(x => Line_(x, "New contacts", r.EvangelismSaturday.NewContacts.ToString()));
                                c.Item().Element(x => Line_(x, "Calls made / reached", $"{r.EvangelismSaturday.Calls} / {r.EvangelismSaturday.Reached}"));
                            });
                            row.RelativeItem().PaddingLeft(12).Column(c =>
                            {
                                c.Item().Element(x => Line_(x, $"{r.FinanceMonth} to date: sessions held", r.EvangelismMonth.Sessions.ToString(), bold: true));
                                c.Item().Element(x => Line_(x, "Present of scheduled", $"{r.EvangelismMonth.Present} of {r.EvangelismMonth.Scheduled} ({r.EvangelismMonth.TurnUpPercent}%)"));
                                c.Item().Element(x => Line_(x, "Flyers handed out", r.EvangelismMonth.Flyers.ToString()));
                                c.Item().Element(x => Line_(x, "Persons ministered to", r.EvangelismMonth.Ministered.ToString()));
                                c.Item().Element(x => Line_(x, "New contacts", r.EvangelismMonth.NewContacts.ToString()));
                                c.Item().Element(x => Line_(x, "Calls made / reached", $"{r.EvangelismMonth.Calls} / {r.EvangelismMonth.Reached}"));
                            });
                        });
                        col.Item().Element(x => Line_(x, "Contacts: being followed up / connected / unreachable", $"{r.ContactsActive} / {r.ContactsConnected} / {r.ContactsUnreachable}"));
                    }

                    // 5. Finance
                    col.Item().PaddingTop(8).Element(c => Heading(c, r.HasEvangelism ? "5. Financial overview" : "4. Financial overview"));
                    col.Item().Element(x => Line_(x, $"Dues collected ({r.FinanceMonth} to date)", Money(r.DuesCollected)));
                    col.Item().Element(x => Line_(x, $"Other collections ({r.FinanceMonth} to date)", Money(r.OtherCollected)));
                    col.Item().Element(x => Line_(x, "Total collected", Money(r.TotalCollected), bold: true));
                    col.Item().Element(x => Line_(x, $"Expenses ({r.ExpensesFrom:d MMM} to {r.ServiceDate:d MMM})", Money(r.Expenses), bold: true));
                    if (r.TopExpenses.Count > 0)
                        col.Item().Element(x => Line_(x, "Largest expense categories", string.Join("  |  ", r.TopExpenses.Select(e => $"{e.Category} {Money(e.Amount)}"))));

                    col.Item().PaddingTop(6).Text("Collections are recorded by month, so they show the month so far. Expenses cover the period since the previous service.")
                        .FontSize(8).FontColor(Muted);
                });

                #endregion

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

    private static void Heading(IContainer c, string text) =>
        c.PaddingTop(4).Text(text.ToUpperInvariant()).FontSize(9).Bold().FontColor(Teal).LetterSpacing(0.08f);

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
