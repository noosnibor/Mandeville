using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Who cares for whom (one carer per member).</summary>
public interface IPastoralAssignmentRepository
{
    Task<List<PastoralAssignmentModel>> SelectPastoralAssignment(Guid? coreMemberKey = null, Guid? memberKey = null, CancellationToken cancellationToken = default);

    /// <summary>Gives a member to a Core Member; a null Core Member removes the current carer.</summary>
    Task<ToastMessageModel> AssignPastoralCare(Guid memberKey, Guid? coreMemberKey, string? who, CancellationToken cancellationToken = default);
}

/// <summary>The private care log kept by Core Members.</summary>
public interface IPastoralContactRepository
{
    Task<List<PastoralContactModel>> SelectPastoralContact(Guid? key = null, Guid? coreMemberKey = null, Guid? memberKey = null, bool? flaggedUnresolved = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdatePastoralContact(PastoralContactModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeletePastoralContact(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public class PastoralAssignmentRepository(IDataAccess dataAccess) : IPastoralAssignmentRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Operations

    /// <inheritdoc />
    public async Task<List<PastoralAssignmentModel>> SelectPastoralAssignment(Guid? coreMemberKey = null, Guid? memberKey = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngCoreMemberKey", coreMemberKey);
        parameters.Add("@flngMemberKey", memberKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<PastoralAssignmentModel>("mandeville.sp_SelectPastoralAssignment", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> AssignPastoralCare(Guid memberKey, Guid? coreMemberKey, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngMemberKey", memberKey);
            parameters.Add("@flngCoreMemberKey", coreMemberKey);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AssignPastoralCare", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success(coreMemberKey is null ? "Carer removed" : "Carer assigned");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}

public class PastoralContactRepository(IDataAccess dataAccess) : IPastoralContactRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<PastoralContactModel>> SelectPastoralContact(Guid? key = null, Guid? coreMemberKey = null, Guid? memberKey = null, bool? flaggedUnresolved = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngCoreMemberKey", coreMemberKey);
        parameters.Add("@flngMemberKey", memberKey);
        parameters.Add("@fblnFlaggedUnresolved", flaggedUnresolved);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<PastoralContactModel>("mandeville.sp_SelectPastoralContact", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdatePastoralContact(PastoralContactModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngCoreMemberKey", obj.flngCoreMemberKey);
            parameters.Add("@flngMemberKey", obj.flngMemberKey);
            parameters.Add("@fdtmContactDate", obj.fdtmContactDate);
            parameters.Add("@fstrMethod", obj.fstrMethod);
            parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            parameters.Add("@fdtmNextContactDate", obj.fdtmNextContactDate);
            parameters.Add("@fblnFlagForMinister", obj.fblnFlagForMinister);
            parameters.Add("@fblnFlagResolved", obj.fblnFlagResolved);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddPastoralContact" : "mandeville.sp_UpdatePastoralContact";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Care contact");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> DeletePastoralContact(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeletePastoralContact", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Care contact");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}
