using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for payment type records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IPaymentTypeRepository
{
    Task<List<PaymentTypeModel>> SelectPaymentType(Guid? key = null, string? search = null, bool? active = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdatePaymentType(PaymentTypeModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeletePaymentType(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class PaymentTypeRepository(IDataAccess dataAccess) : IPaymentTypeRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<PaymentTypeModel>> SelectPaymentType(Guid? key = null, string? search = null, bool? active = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<PaymentTypeModel>("mandeville.sp_SelectPaymentType", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdatePaymentType(PaymentTypeModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@fblnIsDues", obj.fblnIsDues);
            }
            else
            {
                parameters.Add("@fstrName", obj.fstrName?.Trim());
                parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
                parameters.Add("@fblnIsDues", obj.fblnIsDues);
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddPaymentType" : "mandeville.sp_UpdatePaymentType";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Payment Type");
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
    public async Task<ToastMessageModel> DeletePaymentType(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeletePaymentType", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Payment Type");
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
