namespace AppTemplate.Services.Dialogs;

/// <summary>
/// Tells the user something failed and points them at the log folder, so they can attach the
/// log to a bug report.
/// </summary>
public interface IErrorDialogService
{
    Task ShowAsync(string title, string message, Exception? exception = null);
}
