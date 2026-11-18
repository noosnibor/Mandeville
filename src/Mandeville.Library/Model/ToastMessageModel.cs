namespace Mandeville.Library.Model;

/// <summary>Outcome of a repository operation, shown to the user as a toast notification.</summary>
public class ToastMessageModel
{
    #region Properties

    public ToastTypeModel ToastType { get; set; }
    public string? Message { get; set; }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => ToastType == ToastTypeModel.Success;

    #endregion
}
