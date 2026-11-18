using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

public partial interface IServiceRepository
{
    /// <summary>Switches QR check-in on for a service with a new token and window.</summary>
    Task<ToastMessageModel> OpenCheckIn(Guid serviceKey, string token, DateTime opens, DateTime closes, string? who, CancellationToken cancellationToken = default);

    /// <summary>Issues a fresh token (the previous one stays valid for one more rotation).</summary>
    Task<ToastMessageModel> RotateCheckInToken(Guid serviceKey, string token, string? who, CancellationToken cancellationToken = default);

    /// <summary>Switches check-in off and clears the tokens.</summary>
    Task<ToastMessageModel> CloseCheckIn(Guid serviceKey, string? who, CancellationToken cancellationToken = default);

    /// <summary>Name-only member search for the public check-in page.</summary>
    Task<List<CheckInMemberModel>> SearchMembersForCheckIn(string token, string search, CancellationToken cancellationToken = default);

    /// <summary>Checks a member in. The member must confirm their birth day and month.</summary>
    Task<ToastMessageModel> CheckInMember(string token, Guid memberKey, int birthDay, int birthMonth, string method = "QR", string? who = null, CancellationToken cancellationToken = default);
}

public partial class ServiceRepository
{
    #region Check-in Operations

    /// <inheritdoc />
    public Task<ToastMessageModel> OpenCheckIn(Guid serviceKey, string token, DateTime opens, DateTime closes, string? who, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", serviceKey);
        parameters.Add("@fstrCheckInToken", token);
        parameters.Add("@fdtmCheckInOpens", opens);
        parameters.Add("@fdtmCheckInCloses", closes);
        return RunAsync("mandeville.sp_OpenCheckIn", parameters, who, "Check-in is open", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ToastMessageModel> RotateCheckInToken(Guid serviceKey, string token, string? who, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", serviceKey);
        parameters.Add("@fstrCheckInToken", token);
        return RunAsync("mandeville.sp_RotateCheckInToken", parameters, who, "QR code refreshed", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ToastMessageModel> CloseCheckIn(Guid serviceKey, string? who, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", serviceKey);
        return RunAsync("mandeville.sp_CloseCheckIn", parameters, who, "Check-in is closed", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<CheckInMemberModel>> SearchMembersForCheckIn(string token, string search, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrToken", token);
        parameters.Add("@fstrSearch", search);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<CheckInMemberModel>("mandeville.sp_SearchMembersForCheckIn", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> CheckInMember(string token, Guid memberKey, int birthDay, int birthMonth, string method = "QR", string? who = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@fstrToken", token);
            parameters.Add("@flngMemberKey", memberKey);
            parameters.Add("@flngBirthDay", birthDay);
            parameters.Add("@flngBirthMonth", birthMonth);
            parameters.Add("@fstrMethod", method);
            parameters.Add("@fstrWho", who ?? "QR check-in");
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_CheckInMember", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success("You are checked in. God bless you!") : FromRowCount(0, true, "check-in");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion

    #region Private Helpers

    /// <summary>Runs a service check-in procedure that takes who/when and returns a toast.</summary>
    private async Task<ToastMessageModel> RunAsync(string procedure, DynamicParameters parameters, string? who, string successMessage, CancellationToken cancellationToken)
    {
        try
        {
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success(successMessage) : FromRowCount(0, false, "service");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}
