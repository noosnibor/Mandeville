using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

public partial interface IVisitorRepository
{
    /// <summary>Assigns (or, with a null member, unassigns) the connection-team member following up a visitor.</summary>
    Task<ToastMessageModel> AssignVisitorConnect(Guid visitorKey, Guid? memberKey, string? who, CancellationToken cancellationToken = default);

    /// <summary>Refers a visitor to the connection team lead with a reason; a null reason cancels the referral.</summary>
    Task<ToastMessageModel> ReferVisitor(Guid visitorKey, string? reason, string? who, CancellationToken cancellationToken = default);

    /// <summary>Team members with the number of open visitors each one has been given.</summary>
    Task<List<ConnectWorkloadModel>> SelectConnectWorkload(Guid? departmentKey = null, CancellationToken cancellationToken = default);
}

public partial class VisitorRepository
{
    #region Connect Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> AssignVisitorConnect(Guid visitorKey, Guid? memberKey, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", visitorKey);
            parameters.Add("@flngFollowUpMemberKey", memberKey);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AssignVisitorConnect", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success("Connector saved") : FromRowCount(0, false, "connector");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> ReferVisitor(Guid visitorKey, string? reason, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", visitorKey);
            parameters.Add("@fstrReferralReason", reason);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_ReferVisitor", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1 ? Success(reason is null ? "Referral cancelled" : "Referred to the connection team lead") : FromRowCount(0, false, "referral");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<List<ConnectWorkloadModel>> SelectConnectWorkload(Guid? departmentKey = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngDepartmentKey", departmentKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ConnectWorkloadModel>("mandeville.sp_SelectConnectWorkload", CommandType.StoredProcedure, parameters),
            cancellationToken);

        var workload = AsSafeList(result);

        // Only people still being followed up as visitors count. Once someone is staged as an incoming member
        // (or becomes a member) they leave the connector's workload, so the count starts again from zero.
        var open = (await SelectVisitor(cancellationToken: cancellationToken))
            .Where(v => v.flngFollowUpMemberKey is not null && v.fstrStatus is "Visitor" or "Regular Visitor")
            .GroupBy(v => v.flngFollowUpMemberKey!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        foreach (var w in workload) w.flngAssigned = open.GetValueOrDefault(w.flngMemberKey);

        // Without a team filter the list only holds people who have someone assigned
        return departmentKey is null ? workload.Where(w => w.flngAssigned > 0).ToList() : workload;
    }

    #endregion
}
