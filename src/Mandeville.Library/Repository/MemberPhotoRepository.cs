using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for member photos (one live photo per member; flngKey is the member's key).</summary>
public partial interface IMemberPhotoRepository
{
    Task<MemberPhotoModel?> SelectMemberPhoto(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>The photo for a celebration card: the member's own, otherwise the spouse's.</summary>
    Task<MemberPhotoModel?> SelectForCard(Guid memberKey, Guid? spouseKey, CancellationToken cancellationToken = default);

    Task<ToastMessageModel> SaveMemberPhoto(Guid memberKey, byte[] photo, string contentType, string? who, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeleteMemberPhoto(Guid memberKey, string? who, CancellationToken cancellationToken = default);
}

#endregion

public partial class MemberPhotoRepository(IDataAccess dataAccess) : IMemberPhotoRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    /// <summary>Photos are shrunk to about 320 px before saving; anything far bigger is refused.</summary>
    private const int __maxBytes = 400 * 1024;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<MemberPhotoModel?> SelectMemberPhoto(Guid memberKey, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", memberKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<MemberPhotoModel>("mandeville.sp_SelectMemberPhoto", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<MemberPhotoModel?> SelectForCard(Guid memberKey, Guid? spouseKey, CancellationToken cancellationToken = default)
    {
        var photo = await SelectMemberPhoto(memberKey, cancellationToken);
        if (photo is null && spouseKey.HasValue)
            photo = await SelectMemberPhoto(spouseKey.Value, cancellationToken);
        return photo;
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> SaveMemberPhoto(Guid memberKey, byte[] photo, string contentType, string? who, CancellationToken cancellationToken = default)
    {
        if (photo is null || photo.Length == 0)
            return Invalid("There is no photo to save");
        if (photo.Length > __maxBytes)
            return Invalid("That photo is too large. Please choose a smaller one");

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", memberKey);
            parameters.Add("@fbinPhoto", photo, DbType.Binary);
            parameters.Add("@fstrContentType", string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SaveMemberPhoto", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Photo saved");
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
    public async Task<ToastMessageModel> DeleteMemberPhoto(Guid memberKey, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", memberKey);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteMemberPhoto", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Photo removed");
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
