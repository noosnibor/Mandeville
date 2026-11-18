using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for expense category records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IExpenseCategoryRepository
{
    Task<List<ExpenseCategoryModel>> SelectExpenseCategory(Guid? key = null, string? search = null, bool? active = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateExpenseCategory(ExpenseCategoryModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteExpenseCategory(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class ExpenseCategoryRepository(IDataAccess dataAccess) : IExpenseCategoryRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<ExpenseCategoryModel>> SelectExpenseCategory(Guid? key = null, string? search = null, bool? active = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ExpenseCategoryModel>("mandeville.sp_SelectExpenseCategory", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateExpenseCategory(ExpenseCategoryModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@fstrName", obj.fstrName?.Trim());
                parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
            }
            else
            {
                parameters.Add("@fstrName", obj.fstrName?.Trim());
                parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddExpenseCategory" : "mandeville.sp_UpdateExpenseCategory";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Expense Category");
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
    public async Task<ToastMessageModel> DeleteExpenseCategory(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteExpenseCategory", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Expense Category");
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
