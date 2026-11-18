using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for visitor records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IVisitorRepository
{
    Task<List<VisitorModel>> SelectVisitor(Guid? key = null, string? status = null, string? ageGroup = null, Guid? followUpMemberKey = null, string? gender = null, string? search = null, DateTime? attendedOn = null, bool unassigned = false, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateVisitor(VisitorModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteVisitor(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class VisitorRepository(IDataAccess dataAccess) : IVisitorRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<VisitorModel>> SelectVisitor(Guid? key = null, string? status = null, string? ageGroup = null, Guid? followUpMemberKey = null, string? gender = null, string? search = null, DateTime? attendedOn = null, bool unassigned = false, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrStatus", status);
        parameters.Add("@fstrAgeGroup", ageGroup);
        parameters.Add("@flngFollowUpMemberKey", followUpMemberKey);
        parameters.Add("@fstrGender", gender);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@fdtmAttendedOn", attendedOn);
        parameters.Add("@fblnUnassigned", unassigned);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<VisitorModel>("mandeville.sp_SelectVisitor", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateVisitor(VisitorModel obj, bool insert = true, CancellationToken cancellationToken = default)
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
                parameters.Add("@fstrFirstname", obj.fstrFirstname?.Trim());
                parameters.Add("@fstrLastname", obj.fstrLastname?.Trim());
                parameters.Add("@fstrGender", obj.fstrGender?.Trim());
                parameters.Add("@fstrAgeGroup", obj.fstrAgeGroup?.Trim());
                parameters.Add("@fstrPhoneNumber", obj.fstrPhoneNumber?.Trim());
                parameters.Add("@fstrEmailAddress", obj.fstrEmailAddress?.Trim());
                parameters.Add("@fstrAddress", obj.fstrAddress?.Trim());
                parameters.Add("@fdtmLastContactDate", obj.fdtmLastContactDate);
                parameters.Add("@fdtmNextContactDate", obj.fdtmNextContactDate);
                parameters.Add("@fstrStatus", obj.fstrStatus?.Trim());
                parameters.Add("@fstrJoinedChurch", obj.fstrJoinedChurch?.Trim());
                parameters.Add("@fstrMembershipTransferNote", obj.fstrMembershipTransferNote?.Trim());
                parameters.Add("@fdtmBaptisedOn", obj.fdtmBaptisedOn);
                parameters.Add("@fdtmClassStartedOn", obj.fdtmClassStartedOn);
                parameters.Add("@fdtmTier2On", obj.fdtmTier2On);
                parameters.Add("@fdtmInterviewOn", obj.fdtmInterviewOn);
                parameters.Add("@fdtmInauguratedOn", obj.fdtmInauguratedOn);
                parameters.Add("@fdtmDateOfBirth", obj.fdtmDateOfBirth);
                parameters.Add("@fstrPath", obj.fstrPath?.Trim());
                parameters.Add("@flngMemberKey", obj.flngMemberKey);
            }
            else
            {
                parameters.Add("@fstrFirstname", obj.fstrFirstname?.Trim());
                parameters.Add("@fstrLastname", obj.fstrLastname?.Trim());
                parameters.Add("@fstrGender", obj.fstrGender?.Trim());
                parameters.Add("@fstrAgeGroup", obj.fstrAgeGroup?.Trim());
                parameters.Add("@fstrPhoneNumber", obj.fstrPhoneNumber?.Trim());
                parameters.Add("@fstrEmailAddress", obj.fstrEmailAddress?.Trim());
                parameters.Add("@fstrAddress", obj.fstrAddress?.Trim());
                parameters.Add("@fdtmLastContactDate", obj.fdtmLastContactDate);
                parameters.Add("@fdtmNextContactDate", obj.fdtmNextContactDate);
                parameters.Add("@fstrStatus", obj.fstrStatus?.Trim());
                parameters.Add("@fstrJoinedChurch", obj.fstrJoinedChurch?.Trim());
                parameters.Add("@fstrMembershipTransferNote", obj.fstrMembershipTransferNote?.Trim());
                parameters.Add("@fdtmBaptisedOn", obj.fdtmBaptisedOn);
                parameters.Add("@fdtmClassStartedOn", obj.fdtmClassStartedOn);
                parameters.Add("@fdtmTier2On", obj.fdtmTier2On);
                parameters.Add("@fdtmInterviewOn", obj.fdtmInterviewOn);
                parameters.Add("@fdtmInauguratedOn", obj.fdtmInauguratedOn);
                parameters.Add("@fdtmDateOfBirth", obj.fdtmDateOfBirth);
                parameters.Add("@fstrPath", obj.fstrPath?.Trim());
                parameters.Add("@flngMemberKey", obj.flngMemberKey);
            }
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddVisitor" : "mandeville.sp_UpdateVisitor";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Visitor");
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
    public async Task<ToastMessageModel> DeleteVisitor(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteVisitor", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Visitor");
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
