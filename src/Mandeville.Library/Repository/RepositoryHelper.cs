using Mandeville.Library.Model;

namespace Mandeville.Library.Repository;

/// <summary>Shared helpers used by every repository (toast builders, cancellation, list safety).</summary>
internal static class RepositoryHelper
{
    #region Toast Message Builders

    /// <summary>Returned when a caller passes a null / empty payload.</summary>
    internal static ToastMessageModel Invalid(string message = "Invalid") =>
        new() { ToastType = ToastTypeModel.Danger, Message = message };

    /// <summary>Converts "rows affected" from an add/update into a user-friendly toast.</summary>
    internal static ToastMessageModel FromRowCount(int rowsAffected, bool insert, string entity)
    {
        if (rowsAffected >= 1)
        {
            return new()
            {
                ToastType = ToastTypeModel.Success,
                Message = insert ? $"{entity} was added successfully" : $"{entity} was updated successfully"
            };
        }

        return new()
        {
            ToastType = ToastTypeModel.Warning,
            Message = insert ? $"Unable to add {entity} to the system" : $"Unable to update the {entity} in the system"
        };
    }

    /// <summary>Converts "rows affected" from a delete into a toast.</summary>
    internal static ToastMessageModel FromDelete(int rowsAffected, string entity) =>
        rowsAffected >= 1
            ? new() { ToastType = ToastTypeModel.Success, Message = $"{entity} was deleted successfully" }
            : new() { ToastType = ToastTypeModel.Warning, Message = $"Unable to delete the {entity}" };

    /// <summary>A success toast with a custom message.</summary>
    internal static ToastMessageModel Success(string message) =>
        new() { ToastType = ToastTypeModel.Success, Message = message };

    /// <summary>Converts an unexpected exception (including SQL THROW messages) into a danger toast.</summary>
    internal static ToastMessageModel FromException(Exception ex, string prefix = "") =>
        new() { ToastType = ToastTypeModel.Danger, Message = prefix + ex.Message };

    #endregion

    #region Cancellation & Result Helpers

    /// <summary>
    /// Lets the caller stop waiting for a data-access call as soon as the token is cancelled.
    /// IDataAccess does not accept a CancellationToken, so Task.WaitAsync is used to honour it.
    /// </summary>
    internal static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Always returns a list (never null) from a Dapper result.</summary>
    internal static List<T> AsSafeList<T>(IEnumerable<T>? result) =>
        result is null ? [] : (result as List<T> ?? [.. result]);

    #endregion
}
