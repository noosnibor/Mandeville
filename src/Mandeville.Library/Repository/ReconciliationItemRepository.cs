using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for reconciliation item records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IReconciliationItemRepository
{
    Task<List<ReconciliationItemModel>> SelectReconciliationItem(Guid? key = null, Guid? reconciliationKey = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddReconciliationItem(ReconciliationItemModel obj, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteReconciliationItem(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class ReconciliationItemRepository(IDataAccess dataAccess) : IReconciliationItemRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<ReconciliationItemModel>> SelectReconciliationItem(Guid? key = null, Guid? reconciliationKey = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngReconciliationKey", reconciliationKey);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ReconciliationItemModel>("mandeville.sp_SelectReconciliationItem", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddReconciliationItem(ReconciliationItemModel obj, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngReconciliationKey", obj.flngReconciliationKey);
            parameters.Add("@fstrItemType", obj.fstrItemType?.Trim());
            parameters.Add("@flngSourceKey", obj.flngSourceKey);
            parameters.Add("@fcurAmount", obj.fcurAmount);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddReconciliationItem", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, true, "Reconciliation Item");
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
    public async Task<ToastMessageModel> DeleteReconciliationItem(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteReconciliationItem", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Reconciliation Item");
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
