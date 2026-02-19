using Microsoft.UI.Xaml.Controls;

namespace AppTemplate.Services.Dialogs;

public interface IDialogService
{
    Task<ContentDialogResult> ShowAsync(string title, string content, string closeButtonText);

    Task<ContentDialogResult> ShowAsync(string title, string content, string primaryButtonText, string closeButtonText);

    Task<ContentDialogResult> ShowAsync(string title, string content, string primaryButtonText, string secondaryButtonText, string closeButtonText);

    Task<bool> ShowConfirmationAsync(string title, string content, string confirmButtonText, string cancelButtonText);

    Task ShowErrorAsync(string title, string message);

    Task ShowInfoAsync(string title, string message);
}
