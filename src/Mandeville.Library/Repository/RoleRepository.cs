using Dapper;
using Mandeville.Data;
using Mandeville.Library.Model;
using System.Data;
using System.Text.Json;
using static Mandeville.Library.Repository.RepositoryHelper;

namespace Mandeville.Library.Repository;

#region Contract

/// <summary>Data access for roles and the pages each role may open (versioned: flngVer = 0 is the live row).</summary>
public interface IRoleRepository
{
    Task<List<RoleModel>> SelectRole(Guid? key = null, string? name = null, bool? active = true, CancellationToken cancellationToken = default);
    Task<List<RolePermissionModel>> SelectRolePermission(Guid? roleKey = null, string? roleName = null, CancellationToken cancellationToken = default);

    /// <summary>Adds a role (insert) or renames one (update, only the label can change).</summary>
    Task<ToastMessageModel> AddOrUpdateRole(RoleModel obj, bool insert = true, CancellationToken cancellationToken = default);

    /// <summary>Soft deletes a role. Refused while people still have it.</summary>
    Task<ToastMessageModel> DeleteRole(Guid key, string? who, CancellationToken cancellationToken = default);

    /// <summary>Replaces the pages a role may open with the given list.</summary>
    Task<ToastMessageModel> SaveRolePermissions(Guid roleKey, IEnumerable<RolePermissionModel> permissions, string? who, CancellationToken cancellationToken = default);
}

#endregion

public class RoleRepository(IDataAccess dataAccess) : IRoleRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<RoleModel>> SelectRole(Guid? key = null, string? name = null, bool? active = true, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrName", name?.Trim());
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<RoleModel>("mandeville.sp_SelectRole", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<RolePermissionModel>> SelectRolePermission(Guid? roleKey = null, string? roleName = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngRoleKey", roleKey);
        parameters.Add("@fstrRoleName", roleName?.Trim());

        var result = await WithCancellation(
            __dataAccess.QueryAsync<RolePermissionModel>("mandeville.sp_SelectRolePermission", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateRole(RoleModel obj, bool insert = true, CancellationToken cancellationToken = default)
    {
        if (obj is null || string.IsNullOrWhiteSpace(obj.fstrLabel))
            return Invalid("Enter a name for the role");

        try
        {
            if (insert && obj.flngKey == Guid.Empty)
                obj.flngKey = Guid.NewGuid();

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrLabel", obj.fstrLabel.Trim());
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen ?? AppClock.Now);

            if (insert)
            {
                parameters.Add("@fstrName", obj.fstrName);
                parameters.Add("@fblnLocked", obj.fblnLocked);
            }

            var procedure = insert ? "mandeville.sp_AddRole" : "mandeville.sp_UpdateRole";

            await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success(insert ? "Role was added successfully" : "Role was updated successfully");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> DeleteRole(Guid key, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", key);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_DeleteRole", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Role was removed");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> SaveRolePermissions(Guid roleKey, IEnumerable<RolePermissionModel> permissions, string? who, CancellationToken cancellationToken = default)
    {
        try
        {
            // The list travels as JSON and is unpacked by the stored procedure
            var json = JsonSerializer.Serialize(permissions.Select(p => new
            {
                page = p.fstrPage,
                tabs = string.IsNullOrWhiteSpace(p.fstrTabs) ? null : p.fstrTabs,
                own = p.fblnOwnOnly
            }));

            var parameters = new DynamicParameters();
            parameters.Add("@flngRoleKey", roleKey);
            parameters.Add("@fstrJson", json);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", AppClock.Now);

            await WithCancellation(
                __dataAccess.ExecuteAsync("mandeville.sp_SaveRolePermissions", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return Success("Access saved");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion
}
