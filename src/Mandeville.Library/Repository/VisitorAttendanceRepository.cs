using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for visitor attendance records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IVisitorAttendanceRepository
{
    Task<List<VisitorAttendanceModel>> SelectVisitorAttendance(Guid? key = null, Guid? visitorKey = null, DateTime? serviceDate = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateVisitorAttendance(VisitorAttendanceModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteVisitorAttendance(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class VisitorAttendanceRepository(IDataAccess dataAccess) : IVisitorAttendanceRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<VisitorAttendanceModel>> SelectVisitorAttendance(Guid? key = null, Guid? visitorKey = null, DateTime? serviceDate = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngVisitorKey", visitorKey);
        parameters.Add("@fdtmServiceDate", serviceDate);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<VisitorAttendanceModel>("mandeville.sp_SelectVisitorAttendance", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateVisitorAttendance(VisitorAttendanceModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@flngVisitorKey", obj.flngVisitorKey);
                parameters.Add("@fdtmServiceDate", obj.fdtmServiceDate);
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            else
            {
                parameters.Add("@fstrAltarReason", obj.fstrAltarReason?.Trim());
                parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddVisitorAttendance" : "mandeville.sp_UpdateVisitorAttendance";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Visitor Attendance");
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

    #region Delete

    /// <inheritdoc />
    public async Task<ToastMessageModel> DeleteVisitorAttendance(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteVisitorAttendance", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Visitor attendance");
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
