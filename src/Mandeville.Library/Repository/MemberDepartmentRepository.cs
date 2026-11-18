using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for member department records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IMemberDepartmentRepository
{
    Task<List<MemberDepartmentModel>> SelectMemberDepartment(Guid? key = null, Guid? memberKey = null, Guid? departmentKey = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddMemberDepartment(MemberDepartmentModel obj, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> SetMemberDepartmentLead(Guid key, bool lead, string? who, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteMemberDepartment(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class MemberDepartmentRepository(IDataAccess dataAccess) : IMemberDepartmentRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<MemberDepartmentModel>> SelectMemberDepartment(Guid? key = null, Guid? memberKey = null, Guid? departmentKey = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngMemberKey", memberKey);
        parameters.Add("@flngDepartmentKey", departmentKey);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<MemberDepartmentModel>("mandeville.sp_SelectMemberDepartment", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddMemberDepartment(MemberDepartmentModel obj, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngMemberKey", obj.flngMemberKey);
            parameters.Add("@flngDepartmentKey", obj.flngDepartmentKey);
            parameters.Add("@fblnLead", obj.fblnLead);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddMemberDepartment", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, true, "Member Department");
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

    /// <summary>Tags (or un-tags) a member as the team lead of one of their departments.</summary>
    public async Task<ToastMessageModel> SetMemberDepartmentLead(Guid key, bool lead, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fblnLead", lead);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SetMemberDepartmentLead", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, false, "Team lead");
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
    public async Task<ToastMessageModel> DeleteMemberDepartment(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteMemberDepartment", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Member Department");
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
