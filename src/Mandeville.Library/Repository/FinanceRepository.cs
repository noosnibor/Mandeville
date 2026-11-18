using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for finance records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IFinanceRepository
{
    Task<List<FinanceModel>> SelectFinance(Guid? key = null, int? month = null, int? year = null, Guid? paymentTypeKey = null, Guid? memberKey = null, string? search = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateFinance(FinanceModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteFinance(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class FinanceRepository(IDataAccess dataAccess) : IFinanceRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<FinanceModel>> SelectFinance(Guid? key = null, int? month = null, int? year = null, Guid? paymentTypeKey = null, Guid? memberKey = null, string? search = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngMonth", month);
        parameters.Add("@flngYear", year);
        parameters.Add("@flngPaymentTypeKey", paymentTypeKey);
        parameters.Add("@flngMemberKey", memberKey);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<FinanceModel>("mandeville.sp_SelectFinance", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateFinance(FinanceModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@flngMemberKey", obj.flngMemberKey);
                parameters.Add("@fstrMemberName", obj.fstrMemberName?.Trim());
                parameters.Add("@fstrPaymentMethod", obj.fstrPaymentMethod?.Trim());
                parameters.Add("@flngPaymentTypeKey", obj.flngPaymentTypeKey);
                parameters.Add("@fcurAmount", obj.fcurAmount);
                parameters.Add("@fstrReceivedBy", obj.fstrReceivedBy?.Trim());
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            else
            {
                parameters.Add("@flngMonth", obj.flngMonth);
                parameters.Add("@flngYear", obj.flngYear);
                parameters.Add("@flngMemberKey", obj.flngMemberKey);
                parameters.Add("@fstrMemberName", obj.fstrMemberName?.Trim());
                parameters.Add("@fstrPaymentMethod", obj.fstrPaymentMethod?.Trim());
                parameters.Add("@flngPaymentTypeKey", obj.flngPaymentTypeKey);
                parameters.Add("@fcurAmount", obj.fcurAmount);
                parameters.Add("@fstrReceivedBy", obj.fstrReceivedBy?.Trim());
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddFinance" : "mandeville.sp_UpdateFinance";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Finance");
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
    public async Task<ToastMessageModel> DeleteFinance(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteFinance", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Finance");
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
