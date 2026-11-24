using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for member records (versioned: flngVer = 0 is the live row).</summary>
public partial interface IMemberRepository
{
    Task<List<MemberModel>> SelectMember(Guid? key = null, Guid? memberTypeKey = null, Guid? serviceAssignmentKey = null, string? membershipStatus = null, string? gender = null, string? search = null, Guid? departmentKey = null, bool? active = true, bool? coreMember = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateMember(MemberModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteMember(Guid key, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class MemberRepository(IDataAccess dataAccess) : IMemberRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<MemberModel>> SelectMember(Guid? key = null, Guid? memberTypeKey = null, Guid? serviceAssignmentKey = null, string? membershipStatus = null, string? gender = null, string? search = null, Guid? departmentKey = null, bool? active = true, bool? coreMember = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngMemberTypeKey", memberTypeKey);
        parameters.Add("@flngServiceAssignmentKey", serviceAssignmentKey);
        parameters.Add("@fstrMembershipStatus", membershipStatus);
        parameters.Add("@fstrGender", gender);
        parameters.Add("@fstrSearch", search);
        parameters.Add("@flngDepartmentKey", departmentKey);
        parameters.Add("@fblnCoreMember", coreMember);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<MemberModel>("mandeville.sp_SelectMember", CommandType.StoredProcedure, parameters),
            cancellationToken);

        // Every member list in the app is shown A to Z by the displayed name (first name, then last name)
        return AsSafeList(result)
            .OrderBy(m => m.fstrFirstname, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.fstrLastname, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    #endregion

    #region Command Operations (Add / Update / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateMember(MemberModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrTitle", string.IsNullOrWhiteSpace(obj.fstrTitle) ? null : obj.fstrTitle.Trim());
            parameters.Add("@fstrFirstname", obj.fstrFirstname?.Trim());
            parameters.Add("@fstrLastname", obj.fstrLastname?.Trim());
            parameters.Add("@fstrGender", obj.fstrGender?.Trim());
            parameters.Add("@fstrEmailAddress", obj.fstrEmailAddress?.Trim());
            parameters.Add("@fstrPhoneNumber", obj.fstrPhoneNumber?.Trim());
            parameters.Add("@fstrHomeAddress", obj.fstrHomeAddress?.Trim());
            parameters.Add("@fdtmDateOfBirth", obj.fdtmDateOfBirth);
            parameters.Add("@fdtmJoinDate", obj.fdtmJoinDate);
            parameters.Add("@fdtmLeaveGroupOn", obj.fdtmLeaveGroupOn);
            parameters.Add("@flngMemberTypeKey", obj.flngMemberTypeKey);
            parameters.Add("@flngServiceAssignmentKey", obj.flngServiceAssignmentKey);
            parameters.Add("@fstrMembershipStatus", obj.fstrMembershipStatus?.Trim());
            parameters.Add("@fstrNotes", obj.fstrNotes?.Trim());
            parameters.Add("@fblnCoreMember", obj.fblnCoreMember);
            parameters.Add("@fstrMaritalStatus", string.IsNullOrWhiteSpace(obj.fstrMaritalStatus) ? "Single" : obj.fstrMaritalStatus.Trim());
            parameters.Add("@flngSpouseMemberKey", obj.flngSpouseMemberKey);
            parameters.Add("@fstrSpouseName", string.IsNullOrWhiteSpace(obj.fstrSpouseName) ? null : obj.fstrSpouseName.Trim());
            parameters.Add("@fdtmAnniversaryDate", obj.fdtmAnniversaryDate);
            parameters.Add("@fstrFormerSpouseName", string.IsNullOrWhiteSpace(obj.fstrFormerSpouseName) ? null : obj.fstrFormerSpouseName.Trim());
            parameters.Add("@fblnDontAnnounce", obj.fblnDontAnnounce);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddMember" : "mandeville.sp_UpdateMember";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Member");
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
    public async Task<ToastMessageModel> DeleteMember(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteMember", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Member");
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
