using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Financial ledger: balance per month, brought forward, and closing of periods.</summary>
public partial interface ILedgerRepository
{
    /// <summary>Builds the twelve months of a year with brought forward, totals and balance.</summary>
    Task<List<LedgerMonthModel>> SelectLedger(int year, CancellationToken cancellationToken = default);

    /// <summary>The opening balance and the month it applies to (the first month of the ledger).</summary>
    Task<(int Year, int Month, decimal Amount)> SelectLedgerStart(CancellationToken cancellationToken = default);

    Task<ToastMessageModel> SetOpeningBalance(int year, int month, decimal amount, string? who, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> ClosePeriod(int year, int month, string? who, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> ReopenPeriod(int year, int month, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class LedgerRepository(IDataAccess dataAccess) : ILedgerRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<LedgerMonthModel>> SelectLedger(int year, CancellationToken cancellationToken = default)
    {
        var periods = await LoadPeriodsAsync(cancellationToken);
        var totals = await LoadTotalsAsync(cancellationToken);
        return Build(year, periods, totals);
    }

    /// <inheritdoc />
    public async Task<(int Year, int Month, decimal Amount)> SelectLedgerStart(CancellationToken cancellationToken = default)
    {
        var periods = await LoadPeriodsAsync(cancellationToken);
        var totals = await LoadTotalsAsync(cancellationToken);
        var (index, amount, _) = FindStart(periods, totals);
        return (index / 12, index % 12 + 1, amount);
    }

    private async Task<List<LedgerPeriodModel>> LoadPeriodsAsync(CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fblnActive", true);
        var result = await WithCancellation(
            __dataAccess.QueryAsync<LedgerPeriodModel>("mandeville.sp_SelectLedgerPeriod", CommandType.StoredProcedure, parameters),
            cancellationToken);
        return AsSafeList(result);
    }

    private async Task<List<LedgerTotalModel>> LoadTotalsAsync(CancellationToken cancellationToken)
    {
        var result = await WithCancellation(
            __dataAccess.QueryAsync<LedgerTotalModel>("mandeville.sp_SelectLedgerTotals", CommandType.StoredProcedure, new DynamicParameters()),
            cancellationToken);
        return AsSafeList(result);
    }

    #endregion

    #region Ledger calculation

    /// <summary>Month index used for ordering: year * 12 + (month - 1).</summary>
    private static int IndexOf(int year, int month) => year * 12 + (month - 1);

    /// <summary>
    /// The first month of the ledger. It is the month that carries the opening balance, otherwise the
    /// earliest month with any record, otherwise the current month.
    /// </summary>
    private static (int Index, decimal Opening, bool Explicit) FindStart(List<LedgerPeriodModel> periods, List<LedgerTotalModel> totals)
    {
        var opening = periods.FirstOrDefault(p => p.fcurOpeningBalance.HasValue);
        if (opening is not null)
            return (IndexOf(opening.flngYear, opening.flngMonth), opening.fcurOpeningBalance!.Value, true);

        var candidates = totals.Select(t => IndexOf(t.flngYear, t.flngMonth))
            .Concat(periods.Select(p => IndexOf(p.flngYear, p.flngMonth)))
            .ToList();

        var today = AppClock.Today;
        return (candidates.Count > 0 ? candidates.Min() : IndexOf(today.Year, today.Month), 0m, false);
    }

    private static List<LedgerMonthModel> Build(int year, List<LedgerPeriodModel> periods, List<LedgerTotalModel> totals)
    {
        var (start, opening, _) = FindStart(periods, totals);
        var today = AppClock.Today;
        var todayIndex = IndexOf(today.Year, today.Month);

        var result = new List<LedgerMonthModel>();
        var carried = opening;
        var allPreviousClosed = true;

        // Walk from the start of the ledger so the brought forward is correct for any year shown.
        var last = IndexOf(year, 12);
        for (var index = start; index <= last; index++)
        {
            var y = index / 12;
            var m = index % 12 + 1;
            var total = totals.FirstOrDefault(t => t.flngYear == y && t.flngMonth == m);
            var period = periods.FirstOrDefault(p => p.flngYear == y && p.flngMonth == m);
            var closed = period?.fstrStatus == "Closed";

            var month = new LedgerMonthModel
            {
                Year = y,
                Month = m,
                BroughtForward = carried,
                Collections = total?.fcurCollections ?? 0,
                Expenses = total?.fcurExpenses ?? 0,
                CollectionRecords = total?.flngCollectionRecords ?? 0,
                ExpenseRecords = total?.flngExpenseRecords ?? 0,
                IsClosed = closed,
                IsCurrent = index == todayIndex,
                IsStart = index == start,
                NotStarted = index > todayIndex && total is null,
                CanClose = !closed && allPreviousClosed && index <= todayIndex,
                ClosedBy = period?.fstrClosedBy,
                ClosedOn = period?.fdtmClosedOn
            };

            carried = month.Balance;
            allPreviousClosed &= closed;
            if (y == year) result.Add(month);
        }

        // Months of the shown year that fall before the ledger starts are listed as not started.
        for (var m = 1; m <= 12; m++)
        {
            if (result.Any(r => r.Month == m)) continue;
            result.Add(new LedgerMonthModel { Year = year, Month = m, NotStarted = true, IsCurrent = IndexOf(year, m) == todayIndex });
        }

        return result.OrderBy(r => r.Month).ToList();
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> SetOpeningBalance(int year, int month, decimal amount, string? who, CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12 || year < 2000)
            return Invalid("Choose a valid month and year");

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", Guid.NewGuid());
            parameters.Add("@flngYear", year);
            parameters.Add("@flngMonth", month);
            parameters.Add("@fcurOpeningBalance", amount);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SetLedgerOpening", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Opening balance was saved");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> ClosePeriod(int year, int month, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            // Recalculate on the server side so the closing figures are never taken from the screen.
            var ledger = await SelectLedger(year, cancellationToken);
            var target = ledger.FirstOrDefault(l => l.Month == month);

            if (target is null || target.NotStarted && !target.CanClose)
                return Invalid("This period cannot be closed yet");
            if (target.IsClosed)
                return Invalid("This period is already closed");
            if (!target.CanClose)
                return Invalid("Close the earlier periods first");

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", Guid.NewGuid());
            parameters.Add("@flngYear", year);
            parameters.Add("@flngMonth", month);
            parameters.Add("@fcurBroughtForward", target.BroughtForward);
            parameters.Add("@fcurCollections", target.Collections);
            parameters.Add("@fcurExpenses", target.Expenses);
            parameters.Add("@fcurClosingBalance", target.Balance);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_CloseLedgerPeriod", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success($"Period closed. {target.Balance:N2} is carried forward");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> ReopenPeriod(int year, int month, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngYear", year);
            parameters.Add("@flngMonth", month);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_ReopenLedgerPeriod", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Period reopened. Later closed periods were reopened too");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    #endregion
}
