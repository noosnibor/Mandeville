using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for service records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IServiceRepository
{
    Task<List<ServiceModel>> SelectService(Guid? key = null, string? search = null, string? token = null, DateTime? fromDate = null, DateTime? toDate = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateService(ServiceModel obj, bool insert = true, CancellationToken cancellationToken = default);

    /// <summary>Removes the service from the lists (the row and its history are kept, it is only marked inactive).</summary>
    Task<ToastMessageModel> DeleteService(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class ServiceRepository(IDataAccess dataAccess) : IServiceRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<ServiceModel>> SelectService(Guid? key = null, string? search = null, string? token = null, DateTime? fromDate = null, DateTime? toDate = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fstrToken", token);
        parameters.Add("@fdtmFrom", fromDate);
        parameters.Add("@fdtmTo", toDate);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ServiceModel>("mandeville.sp_SelectService", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateService(ServiceModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@fstrServiceName", obj.fstrServiceName?.Trim());
                parameters.Add("@fdtmServiceDate", obj.fdtmServiceDate);
                parameters.Add("@ftimStartTime", obj.ftimStartTime);
                parameters.Add("@fstrLocation", obj.fstrLocation?.Trim());
            }
            else
            {
                parameters.Add("@fstrServiceName", obj.fstrServiceName?.Trim());
                parameters.Add("@fdtmServiceDate", obj.fdtmServiceDate);
                parameters.Add("@ftimStartTime", obj.ftimStartTime);
                parameters.Add("@fstrLocation", obj.fstrLocation?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddService" : "mandeville.sp_UpdateService";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Service");
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
    public async Task<ToastMessageModel> DeleteService(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteService", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Service");
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
