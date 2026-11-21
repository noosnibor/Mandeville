using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for evangelism service records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IEvangelismServiceRepository
{
    Task<List<EvangelismServiceModel>> SelectEvangelismService(Guid? key = null, string? search = null, DateTime? fromDate = null, DateTime? toDate = null, bool? active = true, string? area = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateEvangelismService(EvangelismServiceModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteEvangelismService(Guid key, string? who, CancellationToken cancellationToken = default);

    /// <summary>The numbers for every active session between two dates (no names).</summary>
    Task<List<EvangelismStatModel>> SelectEvangelismStat(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

#endregion

public partial class EvangelismServiceRepository(IDataAccess dataAccess) : IEvangelismServiceRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<EvangelismServiceModel>> SelectEvangelismService(Guid? key = null, string? search = null, DateTime? fromDate = null, DateTime? toDate = null, bool? active = true, string? area = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fdtmFrom", fromDate);
        parameters.Add("@fdtmTo", toDate);
        parameters.Add("@fblnActive", active);
        parameters.Add("@fstrArea", area);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismServiceModel>("mandeville.sp_SelectEvangelismService", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<EvangelismStatModel>> SelectEvangelismStat(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmFrom", fromDate.Date);
        parameters.Add("@fdtmTo", toDate.Date);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismStatModel>("mandeville.sp_SelectEvangelismStat", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateEvangelismService(EvangelismServiceModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fdtmServiceDate", obj.fdtmServiceDate);
            parameters.Add("@ftimStartTime", obj.ftimStartTime);
            parameters.Add("@fstrLocation", obj.fstrLocation?.Trim());
            parameters.Add("@flngTotalPresent", obj.flngTotalPresent);
            parameters.Add("@flngTotalCallers", obj.flngTotalCallers);
            parameters.Add("@flngTotalPrayers", obj.flngTotalPrayers);
            parameters.Add("@flngTotalMinistered", obj.flngTotalMinistered);
            parameters.Add("@fstrArea", string.IsNullOrWhiteSpace(obj.fstrArea) ? EvangelismRules.Street : obj.fstrArea.Trim());
            parameters.Add("@flngLeadMemberKey", obj.flngLeadMemberKey);
            parameters.Add("@flngSlots", obj.flngSlots);
            parameters.Add("@flngFlyers", obj.flngFlyers);
            parameters.Add("@fstrNotes", string.IsNullOrWhiteSpace(obj.fstrNotes) ? null : obj.fstrNotes.Trim());
            parameters.Add("@fblnResultsDone", obj.fblnResultsDone);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddEvangelismService" : "mandeville.sp_UpdateEvangelismService";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Evangelism Service");
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
    public async Task<ToastMessageModel> DeleteEvangelismService(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteEvangelismService", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Evangelism Service");
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
