using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for expenditure records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IExpenditureRepository
{
    Task<List<ExpenditureModel>> SelectExpenditure(Guid? key = null, Guid? expenseCategoryKey = null, string? search = null, DateTime? fromDate = null, DateTime? toDate = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateExpenditure(ExpenditureModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteExpenditure(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class ExpenditureRepository(IDataAccess dataAccess) : IExpenditureRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<ExpenditureModel>> SelectExpenditure(Guid? key = null, Guid? expenseCategoryKey = null, string? search = null, DateTime? fromDate = null, DateTime? toDate = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngExpenseCategoryKey", expenseCategoryKey);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fdtmFrom", fromDate);
        parameters.Add("@fdtmTo", toDate);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ExpenditureModel>("mandeville.sp_SelectExpenditure", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateExpenditure(ExpenditureModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            if (insert)
            {
                parameters.Add("@fdtmExpenseDate", obj.fdtmExpenseDate);
                parameters.Add("@flngExpenseCategoryKey", obj.flngExpenseCategoryKey);
                parameters.Add("@fstrPayee", obj.fstrPayee?.Trim());
                parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
                parameters.Add("@fstrPaymentMethod", obj.fstrPaymentMethod?.Trim());
                parameters.Add("@fstrReferenceNumber", obj.fstrReferenceNumber?.Trim());
                parameters.Add("@fcurAmount", obj.fcurAmount);
                parameters.Add("@fstrPaidBy", obj.fstrPaidBy?.Trim());
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            else
            {
                parameters.Add("@fdtmExpenseDate", obj.fdtmExpenseDate);
                parameters.Add("@flngExpenseCategoryKey", obj.flngExpenseCategoryKey);
                parameters.Add("@fstrPayee", obj.fstrPayee?.Trim());
                parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
                parameters.Add("@fstrPaymentMethod", obj.fstrPaymentMethod?.Trim());
                parameters.Add("@fstrReferenceNumber", obj.fstrReferenceNumber?.Trim());
                parameters.Add("@fcurAmount", obj.fcurAmount);
                parameters.Add("@fstrPaidBy", obj.fstrPaidBy?.Trim());
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddExpenditure" : "mandeville.sp_UpdateExpenditure";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Expenditure");
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
    public async Task<ToastMessageModel> DeleteExpenditure(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteExpenditure", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Expenditure");
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
