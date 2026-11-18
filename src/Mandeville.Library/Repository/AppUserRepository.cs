using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for sign-in accounts (versioned: flngVer = 0 is the live row).</summary>
public interface IAppUserRepository
{
    Task<List<AppUserModel>> SelectAppUser(Guid? key = null, string? email = null, bool? active = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateAppUser(AppUserModel obj, bool insert = true, CancellationToken cancellationToken = default);

    /// <summary>Records a sign-in attempt (clears or increases the failed-attempt count and may lock the account).</summary>
    Task RecordSignIn(Guid key, bool success, int maxAttempts = 5, int lockMinutes = 10, CancellationToken cancellationToken = default);
}

#endregion

public class AppUserRepository(IDataAccess dataAccess) : IAppUserRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<AppUserModel>> SelectAppUser(Guid? key = null, string? email = null, bool? active = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrEmail", email?.Trim());
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<AppUserModel>("mandeville.sp_SelectAppUser", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateAppUser(AppUserModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrEmail", obj.fstrEmail?.Trim().ToLowerInvariant());
            parameters.Add("@fstrDisplayName", obj.fstrDisplayName?.Trim());
            parameters.Add("@fstrPasswordHash", obj.fstrPasswordHash);
            parameters.Add("@fstrRole", obj.fstrRole);
            parameters.Add("@flngMemberKey", obj.flngMemberKey);
            parameters.Add("@fblnMustChangePassword", obj.fblnMustChangePassword);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddAppUser" : "mandeville.sp_UpdateAppUser";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Account");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task RecordSignIn(Guid key, bool success, int maxAttempts = 5, int lockMinutes = 10, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fblnSuccess", success);
        parameters.Add("@fdtmWhen", AppClock.Now);
        parameters.Add("@flngMaxAttempts", maxAttempts);
        parameters.Add("@flngLockMinutes", lockMinutes);

        await WithCancellation(
            __dataAccess.ExecuteAsync("mandeville.sp_RecordSignIn", CommandType.StoredProcedure, parameters),
            cancellationToken);
    }

    #endregion
}
