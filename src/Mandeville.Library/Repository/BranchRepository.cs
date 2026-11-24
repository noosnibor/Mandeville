using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for branch records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IBranchRepository
{
    Task<List<BranchModel>> SelectBranch(Guid? key = null, string? search = null, bool? active = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateBranch(BranchModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteBranch(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class BranchRepository(IDataAccess dataAccess) : IBranchRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<BranchModel>> SelectBranch(Guid? key = null, string? search = null, bool? active = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<BranchModel>("mandeville.sp_SelectBranch", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateBranch(BranchModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrName", obj.fstrName?.Trim());
            parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddBranch" : "mandeville.sp_UpdateBranch";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Branch");
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
    public async Task<ToastMessageModel> DeleteBranch(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteBranch", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Branch");
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
