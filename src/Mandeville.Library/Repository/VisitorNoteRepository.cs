using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for the notes kept per visitor (versioned: flngVer = 0 is the live row).</summary>
public partial interface IVisitorNoteRepository
{
    Task<List<VisitorNoteModel>> SelectVisitorNote(Guid? key = null, Guid? visitorKey = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddVisitorNote(VisitorNoteModel obj, CancellationToken cancellationToken = default);
}

#endregion

public partial class VisitorNoteRepository(IDataAccess dataAccess) : IVisitorNoteRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<VisitorNoteModel>> SelectVisitorNote(Guid? key = null, Guid? visitorKey = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@flngVisitorKey", visitorKey);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<VisitorNoteModel>("mandeville.sp_SelectVisitorNote", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddVisitorNote(VisitorNoteModel obj, CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            if (obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngVisitorKey", obj.flngVisitorKey);
            parameters.Add("@fdtmNoteDate", obj.fdtmNoteDate);
            parameters.Add("@fstrNote", obj.fstrNote?.Trim());
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_AddVisitorNote", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, true, "Note");
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
