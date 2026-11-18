using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for reconciliation records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IReconciliationRepository
{
    Task<List<ReconciliationModel>> SelectReconciliation(Guid? key = null, int? month = null, int? year = null, string? status = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateReconciliation(ReconciliationModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteReconciliation(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class ReconciliationRepository(IDataAccess dataAccess) : IReconciliationRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<ReconciliationModel>> SelectReconciliation(Guid? key = null, int? month = null, int? year = null, string? status = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngMonth", month);
        parameters.Add("@flngYear", year);
        parameters.Add("@fstrStatus", status);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ReconciliationModel>("mandeville.sp_SelectReconciliation", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateReconciliation(ReconciliationModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@flngMonth", obj.flngMonth);
                parameters.Add("@flngYear", obj.flngYear);
                parameters.Add("@fcurOpeningBalance", obj.fcurOpeningBalance);
                parameters.Add("@fcurStatementBalance", obj.fcurStatementBalance);
                parameters.Add("@fstrVarianceNote", obj.fstrVarianceNote?.Trim());
            }
            else
            {
                parameters.Add("@fcurOpeningBalance", obj.fcurOpeningBalance);
                parameters.Add("@fcurStatementBalance", obj.fcurStatementBalance);
                parameters.Add("@fstrVarianceNote", obj.fstrVarianceNote?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddReconciliation" : "mandeville.sp_UpdateReconciliation";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Reconciliation");
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
    public async Task<ToastMessageModel> DeleteReconciliation(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteReconciliation", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Reconciliation");
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
