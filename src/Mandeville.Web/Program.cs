using Mandeville.Data;
using Mandeville.Web.Components;
using Mandeville.Library.Repository;
using Mandeville.Web.Reports;
using Mandeville.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // Member photos come back from the browser as small pictures (about 25 KB) and celebration card images as larger ones (up to a few hundred KB);
    // the default 32 KB message limit is too tight for them
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 4 * 1024 * 1024);

// Sign-in: cookie authentication, roles and the signed-in name for fstrWho.
builder.Services.AddMemoryCache();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "wafif.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/account/signin";
        options.AccessDeniedPath = "/account/denied";
        options.Events.OnValidatePrincipal = CookieEvents.ValidateAsync;
    });
builder.Services.AddAuthorization();
// Every page's [PageAccess] line turns into a "page:xxx" rule, answered from the access claims in the sign-in cookie
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PageAccessPolicyProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CircuitHandler, UserCircuitHandler>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ServiceReportBuilder>();
builder.Services.AddScoped<PeriodReportBuilder>();
builder.Services.AddScoped<ReportMailer>();
builder.Services.AddSingleton<Mandeville.Web.Celebrations.CelebrationMailer>();
builder.Services.AddSingleton<Mandeville.Web.Celebrations.CelebrationEmailStatus>();

// Data access (Dapper + SQL Server) and one repository per table.
builder.Services.AddScoped<IDataAccess, DataAccess>();
builder.Services.AddScoped<IMemberTypeRepository, MemberTypeRepository>();
builder.Services.AddScoped<IServiceAssignmentRepository, ServiceAssignmentRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IPaymentTypeRepository, PaymentTypeRepository>();
builder.Services.AddScoped<IMessageTemplateRepository, MessageTemplateRepository>();
builder.Services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IMemberDepartmentRepository, MemberDepartmentRepository>();
builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IVisitorAttendanceRepository, VisitorAttendanceRepository>();
builder.Services.AddScoped<IVisitorNoteRepository, VisitorNoteRepository>();
builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
builder.Services.AddScoped<IExpenditureRepository, ExpenditureRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<ICelebrationRepository, CelebrationRepository>();
builder.Services.AddScoped<IMemberPhotoRepository, MemberPhotoRepository>();
builder.Services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
builder.Services.AddScoped<IReconciliationItemRepository, ReconciliationItemRepository>();
builder.Services.AddScoped<IEvangelismServiceRepository, EvangelismServiceRepository>();
builder.Services.AddScoped<IEvangelismAssignmentRepository, EvangelismAssignmentRepository>();
builder.Services.AddScoped<IEvangelismAvailabilityRepository, EvangelismAvailabilityRepository>();
builder.Services.AddScoped<IEvangelismContactRepository, EvangelismContactRepository>();
builder.Services.AddScoped<Mandeville.Web.Outreach.EvangelismMailer>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<ILookupValueRepository, LookupValueRepository>();
builder.Services.AddScoped<Mandeville.Web.Dashboards.RoleStatsService>();
builder.Services.AddScoped<Mandeville.Web.Dashboards.FollowUpBadge>();
builder.Services.AddScoped<IMemberAttendanceRepository, MemberAttendanceRepository>();
builder.Services.AddScoped<IVisitingAttendanceRepository, VisitingAttendanceRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPastoralAssignmentRepository, PastoralAssignmentRepository>();
builder.Services.AddScoped<IPastoralContactRepository, PastoralContactRepository>();

var app = builder.Build();

// First start: create the first administrator when there are no accounts yet.
using (var scope = app.Services.CreateScope())
{
    try
    {
        // The starting roles (with the access they already had) are made once, before anyone signs in
        await AccessSeeder.EnsureAsync(scope.ServiceProvider.GetRequiredService<IRoleRepository>(), app.Logger);

        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        await accounts.EnsureFirstAdminAsync();
        await accounts.EnsureSysAdminAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not check or create the first administrator account.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Behind Azure's front door the app sees plain http; this makes it use the real https address the visitor typed,
// so links built from it (like the check-in QR address) are correct.
var forwarded = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwarded.KnownIPNetworks.Clear();
forwarded.KnownProxies.Clear();
app.UseForwardedHeaders(forwarded);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Signing out ends the cookie and returns to the sign-in page.
app.MapGet("/account/signout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/account/signin");
});

// The service report PDF (anyone whose role can open the Reports page).
app.MapGet("/reports/service/{serviceKey:guid}", async (Guid serviceKey, ServiceReportBuilder reportBuilder, CancellationToken cancellationToken) =>
{
    var report = await reportBuilder.BuildAsync(serviceKey, cancellationToken);
    return report is null
        ? Results.NotFound()
        : Results.File(ServiceReportPdf.Create(report), "application/pdf", report.FileName);
}).RequireAuthorization(new AuthorizeAttribute { Policy = PageAccessPolicy.For(PageKeys.Reports) });

// The week / month / quarter / year / custom report PDF, e.g. /reports/period?kind=Month&from=2026-11-01&to=2026-11-30
app.MapGet("/reports/period", async (string? kind, DateTime from, DateTime to, PeriodReportBuilder reportBuilder, CancellationToken cancellationToken) =>
{
    var report = await reportBuilder.BuildAsync(kind ?? PeriodKinds.Custom, from, to, cancellationToken);
    return Results.File(PeriodReportPdf.Create(report), "application/pdf", report.FileName);
}).RequireAuthorization(new AuthorizeAttribute { Policy = PageAccessPolicy.For(PageKeys.Reports) });

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Put the editable fixed lists (saved in Setup) into use; the app still starts if the database has not been republished yet
try
{
    using var listScope = app.Services.CreateScope();
    await listScope.ServiceProvider.GetRequiredService<ILookupValueRepository>().LoadFixedListsAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "The editable fixed lists could not be loaded, the built-in values are used.");
}

app.Run();
