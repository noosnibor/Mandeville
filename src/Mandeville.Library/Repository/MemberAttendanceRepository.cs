using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for member attendance records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IMemberAttendanceRepository
{
    Task<List<MemberAttendanceModel>> SelectMemberAttendance(Guid? key = null, Guid? serviceKey = null, Guid? memberKey = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateMemberAttendance(MemberAttendanceModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteMemberAttendance(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class MemberAttendanceRepository(IDataAccess dataAccess) : IMemberAttendanceRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<MemberAttendanceModel>> SelectMemberAttendance(Guid? key = null, Guid? serviceKey = null, Guid? memberKey = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngServiceKey", serviceKey);
        parameters.Add("@flngMemberKey", memberKey);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<MemberAttendanceModel>("mandeville.sp_SelectMemberAttendance", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateMemberAttendance(MemberAttendanceModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@flngServiceKey", obj.flngServiceKey);
                parameters.Add("@flngMemberKey", obj.flngMemberKey);
                parameters.Add("@fdtmCheckedInAt", obj.fdtmCheckedInAt);
                parameters.Add("@fstrMethod", obj.fstrMethod?.Trim());
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            else
            {
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddMemberAttendance" : "mandeville.sp_UpdateMemberAttendance";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Member Attendance");
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
    public async Task<ToastMessageModel> DeleteMemberAttendance(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteMemberAttendance", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Member Attendance");
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
