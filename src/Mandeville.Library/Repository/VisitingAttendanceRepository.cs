using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for WAFIF members from other branches who visit this branch (versioned: flngVer = 0 is the live row).</summary>
public interface IVisitingAttendanceRepository
{
    /// <summary>Visiting members present at a service (or at every service when no key is given).</summary>
    Task<List<VisitingAttendanceModel>> SelectVisitingAttendance(Guid? serviceKey = null, bool? active = true, CancellationToken cancellationToken = default);

    /// <summary>Visiting members who have been here before, most recent visit first.</summary>
    Task<List<VisitingMemberModel>> SelectKnownVisitors(CancellationToken cancellationToken = default);

    /// <summary>Marks a visiting member present. A returning visitor (same name and branch) is found, not created again.</summary>
    Task<ToastMessageModel> AddVisitingAttendance(Guid serviceKey, string? title, string firstname, string lastname, string branch, string method, string? who, CancellationToken cancellationToken = default);

    /// <summary>Removes a visiting member from a service (soft delete).</summary>
    Task<ToastMessageModel> DeleteVisitingAttendance(Guid key, string? who, CancellationToken cancellationToken = default);

    /// <summary>Name and branch search of known visiting members for the public check-in page.</summary>
    Task<List<CheckInVisitingModel>> SearchVisitingForCheckIn(string token, string search, CancellationToken cancellationToken = default);

    /// <summary>Checks a known visiting member in from the public check-in page.</summary>
    Task<ToastMessageModel> CheckInVisiting(string token, Guid visitingMemberKey, string method = "QR", string? who = null, CancellationToken cancellationToken = default);
}

#endregion

public class VisitingAttendanceRepository(IDataAccess dataAccess) : IVisitingAttendanceRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<VisitingAttendanceModel>> SelectVisitingAttendance(Guid? serviceKey = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngServiceKey", serviceKey);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<VisitingAttendanceModel>("mandeville.sp_SelectVisitingAttendance", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<VisitingMemberModel>> SelectKnownVisitors(CancellationToken cancellationToken = default)
    {
        var result = await WithCancellation(
            __dataAccess.QueryAsync<VisitingMemberModel>("mandeville.sp_SelectKnownVisitors", CommandType.StoredProcedure),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<CheckInVisitingModel>> SearchVisitingForCheckIn(string token, string search, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrToken", token);
        parameters.Add("@fstrSearch", search);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<CheckInVisitingModel>("mandeville.sp_SearchVisitingForCheckIn", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddVisitingAttendance(Guid serviceKey, string? title, string firstname, string lastname, string branch, string method, string? who, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname) || string.IsNullOrWhiteSpace(branch))
            return Invalid("Please enter the first name, last name and branch");

        try
        {
            var now = AppClock.Now;
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", Guid.NewGuid());
            parameters.Add("@flngServiceKey", serviceKey);
            parameters.Add("@fstrTitle", string.IsNullOrWhiteSpace(title) ? null : title.Trim());
            parameters.Add("@fstrFirstname", firstname.Trim());
            parameters.Add("@fstrLastname", lastname.Trim());
            parameters.Add("@fstrBranch", branch.Trim());
            parameters.Add("@fdtmCheckedInAt", now);
            parameters.Add("@fstrMethod", method);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddVisitingAttendance", CommandType.StoredProcedure, parameters),
                cancellationToken);

            var name = string.Join(' ', new[] { title, firstname.Trim(), lastname.Trim() }.Where(p => !string.IsNullOrWhiteSpace(p)));
            return rows >= 1 ? Success($"{name} ({branch.Trim()}) is marked present.") : FromRowCount(0, true, "Visiting member");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> DeleteVisitingAttendance(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteVisitingAttendance", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Visiting attendance");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> CheckInVisiting(string token, Guid visitingMemberKey, string method = "QR", string? who = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@fstrToken", token);
            parameters.Add("@flngVisitingMemberKey", visitingMemberKey);
            parameters.Add("@fstrMethod", method);
            parameters.Add("@fstrWho", who ?? "QR check-in");
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_CheckInVisiting", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success("You are checked in. God bless you!") : FromRowCount(0, true, "check-in");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}
