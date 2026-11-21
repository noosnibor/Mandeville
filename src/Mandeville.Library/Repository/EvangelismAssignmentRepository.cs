using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for evangelism assignment records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IEvangelismAssignmentRepository
{
    Task<List<EvangelismAssignmentModel>> SelectEvangelismAssignment(Guid? key = null, Guid? serviceKey = null, Guid? memberKey = null, string? role = null, string? search = null, bool? active = true, DateTime? fromDate = null, DateTime? toDate = null, string? status = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateEvangelismAssignment(EvangelismAssignmentModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteEvangelismAssignment(Guid key, string? who, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets Confirmed, Declined, Attended or Absent. When <paramref name="memberKey"/> is given the assignment must be that member's own.
    /// </summary>
    Task<ToastMessageModel> SetEvangelismAssignmentStatus(Guid key, string status, string? who, Guid? memberKey = null, CancellationToken cancellationToken = default);
}

#endregion

public partial class EvangelismAssignmentRepository(IDataAccess dataAccess) : IEvangelismAssignmentRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<EvangelismAssignmentModel>> SelectEvangelismAssignment(Guid? key = null, Guid? serviceKey = null, Guid? memberKey = null, string? role = null, string? search = null, bool? active = true, DateTime? fromDate = null, DateTime? toDate = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngServiceKey", serviceKey);
        parameters.Add("@flngMemberKey", memberKey);
        parameters.Add("@fstrRole", role);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fblnActive", active);
        parameters.Add("@fdtmFrom", fromDate);
        parameters.Add("@fdtmTo", toDate);
        parameters.Add("@fstrStatus", status);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismAssignmentModel>("mandeville.sp_SelectEvangelismAssignment", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateEvangelismAssignment(EvangelismAssignmentModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngServiceKey", obj.flngServiceKey);
            parameters.Add("@flngMemberKey", obj.flngMemberKey);
            parameters.Add("@fstrTitle", obj.fstrTitle?.Trim());
            parameters.Add("@fstrFirstname", obj.fstrFirstname?.Trim());
            parameters.Add("@fstrLastname", obj.fstrLastname?.Trim());
            parameters.Add("@fstrEmailAddress", obj.fstrEmailAddress?.Trim());
            parameters.Add("@fstrSchedule", obj.fstrSchedule?.Trim());
            parameters.Add("@fstrAssignment", obj.fstrAssignment?.Trim());
            parameters.Add("@fstrRole", obj.fstrRole?.Trim());
            parameters.Add("@fstrStatus", string.IsNullOrWhiteSpace(obj.fstrStatus) ? EvangelismRules.Scheduled : obj.fstrStatus);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddEvangelismAssignment" : "mandeville.sp_UpdateEvangelismAssignment";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Evangelism Assignment");
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
    public async Task<ToastMessageModel> DeleteEvangelismAssignment(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteEvangelismAssignment", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Evangelism Assignment");
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

    #region Answering

    /// <inheritdoc />
    public async Task<ToastMessageModel> SetEvangelismAssignmentStatus(Guid key, string status, string? who, Guid? memberKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@flngMemberKey", memberKey);
            parameters.Add("@fstrStatus", status);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SetEvangelismAssignmentStatus", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, false, "Evangelism Assignment");
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
