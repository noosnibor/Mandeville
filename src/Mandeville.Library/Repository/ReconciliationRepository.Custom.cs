using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

public partial interface IReconciliationRepository
{
    /// <summary>Collections and expenses up to the end of the month that are not yet on a finalised reconciliation.</summary>
    Task<List<ReconciliationCandidateModel>> SelectCandidates(int month, int year, Guid? reconciliationKey = null, CancellationToken cancellationToken = default);

    /// <summary>Locks the reconciliation (and its collections/expenses). A difference needs a variance note.</summary>
    Task<ToastMessageModel> FinalizeReconciliation(Guid key, string? varianceNote, string reconciledBy, CancellationToken cancellationToken = default);

    /// <summary>Returns a finalised reconciliation to Draft so it can be corrected.</summary>
    Task<ToastMessageModel> ReopenReconciliation(Guid key, string? who, CancellationToken cancellationToken = default);
}

public partial class ReconciliationRepository
{
    #region Workflow Operations

    /// <inheritdoc />
    public async Task<List<ReconciliationCandidateModel>> SelectCandidates(int month, int year, Guid? reconciliationKey = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngMonth", month);
        parameters.Add("@flngYear", year);
        parameters.Add("@flngReconciliationKey", reconciliationKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ReconciliationCandidateModel>("mandeville.sp_SelectReconciliationCandidates", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> FinalizeReconciliation(Guid key, string? varianceNote, string reconciledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrVarianceNote", string.IsNullOrWhiteSpace(varianceNote) ? null : varianceNote.Trim());
            parameters.Add("@fstrReconciledBy", reconciledBy);
            parameters.Add("@fdtmReconciledOn", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_FinalizeReconciliation", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success("Reconciliation finalised") : FromRowCount(0, false, "reconciliation");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> ReopenReconciliation(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_ReopenReconciliation", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success("Reconciliation reopened") : FromRowCount(0, false, "reconciliation");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}
