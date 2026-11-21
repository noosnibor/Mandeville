using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for the monthly invites (one private link per member) and the Saturdays each member said they can serve.</summary>
public partial interface IEvangelismAvailabilityRepository
{
    /// <summary>Invites for a month, or the one with this private token, or one member's. The month is any date in it.</summary>
    Task<List<EvangelismInviteModel>> SelectEvangelismInvite(DateTime? month = null, string? token = null, Guid? memberKey = null, CancellationToken cancellationToken = default);

    /// <summary>Makes an invite for every active team member who does not have one for the month yet.</summary>
    Task EnsureEvangelismInvites(DateTime month, Guid? departmentKey, string? who, CancellationToken cancellationToken = default);

    /// <summary>The Saturday and area choices for a month (members who said "not available" have none).</summary>
    Task<List<EvangelismChoiceModel>> SelectEvangelismChoice(DateTime? month = null, Guid? inviteKey = null, CancellationToken cancellationToken = default);

    /// <summary>Replaces a member's answer for the month.</summary>
    Task<ToastMessageModel> SaveEvangelismAvailability(Guid inviteKey, bool notAvailable, IEnumerable<(DateTime Date, string Area)> choices, string? who, CancellationToken cancellationToken = default);

    Task MarkEvangelismInviteSent(Guid inviteKey, CancellationToken cancellationToken = default);
}

#endregion

public partial class EvangelismAvailabilityRepository(IDataAccess dataAccess) : IEvangelismAvailabilityRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<EvangelismInviteModel>> SelectEvangelismInvite(DateTime? month = null, string? token = null, Guid? memberKey = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmMonth", month is null ? null : EvangelismRules.MonthOf(month.Value));
        parameters.Add("@fstrToken", token);
        parameters.Add("@flngMemberKey", memberKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismInviteModel>("mandeville.sp_SelectEvangelismInvite", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<EvangelismChoiceModel>> SelectEvangelismChoice(DateTime? month = null, Guid? inviteKey = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmMonth", month is null ? null : EvangelismRules.MonthOf(month.Value));
        parameters.Add("@flngInviteKey", inviteKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismChoiceModel>("mandeville.sp_SelectEvangelismAvailability", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task EnsureEvangelismInvites(DateTime month, Guid? departmentKey, string? who, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmMonth", EvangelismRules.MonthOf(month));
        parameters.Add("@flngDepartmentKey", departmentKey);
        parameters.Add("@fstrWho", who);
        parameters.Add("@fdtmWhen", AppClock.Now);

        await WithCancellation(
            __dataAccess.ExecuteAsync("mandeville.sp_CreateEvangelismInvites", CommandType.StoredProcedure, parameters),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> SaveEvangelismAvailability(Guid inviteKey, bool notAvailable, IEnumerable<(DateTime Date, string Area)> choices, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            // "yyyy-MM-dd|Area;yyyy-MM-dd|Area"
            var text = string.Join(";", choices.Select(c => $"{c.Date:yyyy-MM-dd}|{c.Area}"));

            var parameters = new DynamicParameters();
            parameters.Add("@flngInviteKey", inviteKey);
            parameters.Add("@fblnNotAvailable", notAvailable);
            parameters.Add("@fstrChoices", text.Length == 0 ? null : text);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SaveEvangelismAvailability", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, false, "Availability");
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
    public async Task MarkEvangelismInviteSent(Guid inviteKey, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", inviteKey);
        parameters.Add("@fdtmWhen", AppClock.Now);

        await WithCancellation(
            __dataAccess.ExecuteAsync("mandeville.sp_MarkEvangelismInviteSent", CommandType.StoredProcedure, parameters),
            cancellationToken);
    }

    #endregion
}
