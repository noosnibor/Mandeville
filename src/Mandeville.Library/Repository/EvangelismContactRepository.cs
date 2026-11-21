using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for the people met on the street and the calls made to them.</summary>
public partial interface IEvangelismContactRepository
{
    Task<List<EvangelismContactModel>> SelectEvangelismContact(Guid? key = null, string? status = null, Guid? callerKey = null, string? search = null, bool includePurged = false, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddOrUpdateEvangelismContact(EvangelismContactModel obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteEvangelismContact(Guid key, string? who, CancellationToken cancellationToken = default);

    /// <summary>Saves one call. The contact's status and failed-call count follow from the outcome (see sp_AddEvangelismAttempt).</summary>
    Task<ToastMessageModel> AddEvangelismAttempt(EvangelismAttemptModel obj, CancellationToken cancellationToken = default);
    Task<List<EvangelismAttemptModel>> SelectEvangelismAttempt(Guid contactKey, CancellationToken cancellationToken = default);

    /// <summary>Removes the personal details of unreachable people whose keep-until date has passed.</summary>
    Task PurgeEvangelismContacts(CancellationToken cancellationToken = default);
}

#endregion

public partial class EvangelismContactRepository(IDataAccess dataAccess) : IEvangelismContactRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<EvangelismContactModel>> SelectEvangelismContact(Guid? key = null, string? status = null, Guid? callerKey = null, string? search = null, bool includePurged = false, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrStatus", status);
        parameters.Add("@flngCallerKey", callerKey);
        parameters.Add("@fstrSearch", string.IsNullOrWhiteSpace(search) ? null : search.Trim());
        parameters.Add("@fblnIncludePurged", includePurged);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismContactModel>("mandeville.sp_SelectEvangelismContact", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<EvangelismAttemptModel>> SelectEvangelismAttempt(Guid contactKey, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngContactKey", contactKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EvangelismAttemptModel>("mandeville.sp_SelectEvangelismAttempt", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateEvangelismContact(EvangelismContactModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null || string.IsNullOrWhiteSpace(obj.fstrFirstname))
            return Invalid();

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrFirstname", obj.fstrFirstname.Trim());
            parameters.Add("@fstrLastname", Clean(obj.fstrLastname));
            parameters.Add("@fstrPhoneNumber", Clean(obj.fstrPhoneNumber));
            parameters.Add("@fstrArea", Clean(obj.fstrArea));
            parameters.Add("@fstrNeed", Clean(obj.fstrNeed));
            parameters.Add("@flngSessionKey", obj.flngSessionKey);
            parameters.Add("@fdtmMet", obj.fdtmMet.Date);
            parameters.Add("@flngCallerKey", obj.flngCallerKey);
            parameters.Add("@fstrStatus", string.IsNullOrWhiteSpace(obj.fstrStatus) ? EvangelismRules.ContactNew : obj.fstrStatus);
            parameters.Add("@fstrNotes", Clean(obj.fstrNotes));
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fblnConsent", obj.fblnConsent);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var procedure = insert ? "mandeville.sp_AddEvangelismContact" : "mandeville.sp_UpdateEvangelismContact";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Contact");
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
    public async Task<ToastMessageModel> DeleteEvangelismContact(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteEvangelismContact", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromDelete(rows, "Contact");
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
    public async Task<ToastMessageModel> AddEvangelismAttempt(EvangelismAttemptModel obj, CancellationToken cancellationToken = default)
    {
        if (obj is null || obj.flngContactKey == Guid.Empty)
            return Invalid();

        try
        {
            if (obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngContactKey", obj.flngContactKey);
            parameters.Add("@flngMemberKey", obj.flngMemberKey);
            parameters.Add("@flngSessionKey", obj.flngSessionKey);
            parameters.Add("@fstrOutcome", obj.fstrOutcome);
            parameters.Add("@fstrNote", string.IsNullOrWhiteSpace(obj.fstrNote) ? null : obj.fstrNote.Trim());
            parameters.Add("@flngMaxFailed", EvangelismRules.MaxFailedCalls);
            parameters.Add("@flngRetentionDays", EvangelismRules.KeepUnreachableDays);
            parameters.Add("@fdtmToday", AppClock.Today);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddEvangelismAttempt", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, true, "Call");
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
    public async Task PurgeEvangelismContacts(CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmToday", AppClock.Today);

        await WithCancellation(
            __dataAccess.ExecuteAsync("mandeville.sp_PurgeEvangelismContacts", CommandType.StoredProcedure, parameters),
            cancellationToken);
    }

    #endregion
}
