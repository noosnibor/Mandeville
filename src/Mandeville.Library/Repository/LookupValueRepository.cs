using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for the editable fixed lists (versioned: flngVer = 0 is the live row).</summary>
public partial interface ILookupValueRepository
{
    Task<List<LookupValueModel>> SelectLookupValue(string? list = null, bool? active = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddLookupValue(LookupValueModel obj, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> UpdateLookupValue(LookupValueModel obj, CancellationToken cancellationToken = default);

    /// <summary>Saves the built-in values of any editable list that has none yet, then puts every list into use.</summary>
    Task LoadFixedListsAsync(CancellationToken cancellationToken = default);
}

#endregion

public partial class LookupValueRepository(IDataAccess dataAccess) : ILookupValueRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<LookupValueModel>> SelectLookupValue(string? list = null, bool? active = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrList", list);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<LookupValueModel>("mandeville.sp_SelectLookupValue", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddLookupValue(LookupValueModel obj, CancellationToken cancellationToken = default)
    {
        if (obj is null || string.IsNullOrWhiteSpace(obj.fstrValue))
            return Invalid();

        try
        {
            if (obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrList", obj.fstrList);
            parameters.Add("@fstrValue", obj.fstrValue.Trim());
            parameters.Add("@flngSort", obj.flngSort);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddLookupValue", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, true, "List value");
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
    public async Task<ToastMessageModel> UpdateLookupValue(LookupValueModel obj, CancellationToken cancellationToken = default)
    {
        if (obj is null || string.IsNullOrWhiteSpace(obj.fstrValue))
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrValue", obj.fstrValue.Trim());
            parameters.Add("@flngSort", obj.flngSort);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_UpdateLookupValue", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, false, "List value");
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

    #region Fixed lists in use

    /// <inheritdoc />
    public async Task LoadFixedListsAsync(CancellationToken cancellationToken = default)
    {
        var saved = await SelectLookupValue(cancellationToken: cancellationToken);

        foreach (var list in FixedLists.EditableNames)
        {
            // First time: save the built-in values so they can be edited
            if (!saved.Any(v => v.fstrList == list))
            {
                var order = 0;
                foreach (var value in FixedLists.Defaults(list))
                    await AddLookupValue(new LookupValueModel { fstrList = list, fstrValue = value, flngSort = order += 10, fstrWho = "System" }, cancellationToken);
                continue;
            }

            FixedLists.Apply(list, saved.Where(v => v.fstrList == list && v.fblnActive).OrderBy(v => v.flngSort).Select(v => v.fstrValue).ToList());
        }
    }

    #endregion
}
