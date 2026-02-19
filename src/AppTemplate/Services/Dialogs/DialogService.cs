using AppTemplate.Shell;
using Microsoft.UI.Xaml.Controls;

namespace AppTemplate.Services.Dialogs;

public class DialogService : IDialogService
{
    private readonly IWindowShellProvider _windowShellProvider;
    private readonly SemaphoreSlim _dialogLock = new(1, 1);

    public DialogService(IWindowShellProvider windowShellProvider)
    {
        _windowShellProvider = windowShellProvider;
    }

    public Task<ContentDialogResult> ShowAsync(string title, string content, string closeButtonText)
    {
        return ShowDialogAsync(title, content, null, null, closeButtonText);
    }

    public Task<ContentDialogResult> ShowAsync(string title, string content, string primaryButtonText, string closeButtonText)
    {
        return ShowDialogAsync(title, content, primaryButtonText, null, closeButtonText);
    }

    public Task<ContentDialogResult> ShowAsync(string title, string content, string primaryButtonText, string secondaryButtonText, string closeButtonText)
    {
        return ShowDialogAsync(title, content, primaryButtonText, secondaryButtonText, closeButtonText);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string content, string confirmButtonText, string cancelButtonText)
    {
        var result = await ShowDialogAsync(title, content, confirmButtonText, null, cancelButtonText);
        return result == ContentDialogResult.Primary;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return ShowAsync(title, message, "OK");
    }

    public Task ShowInfoAsync(string title, string message)
    {
        return ShowAsync(title, message, "OK");
    }

    private async Task<ContentDialogResult> ShowDialogAsync(
        string title,
        string content,
        string? primaryButtonText,
        string? secondaryButtonText,
        string closeButtonText)
    {
        var xamlRoot = _windowShellProvider.Shell?.XamlRoot;
        if (xamlRoot is null)
        {
            return ContentDialogResult.None;
        }

        await _dialogLock.WaitAsync();
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText,
                XamlRoot = xamlRoot,
                DefaultButton = primaryButtonText is not null ? ContentDialogButton.Primary : ContentDialogButton.Close
            };

            if (primaryButtonText is not null)
            {
                dialog.PrimaryButtonText = primaryButtonText;
            }

            if (secondaryButtonText is not null)
            {
                dialog.SecondaryButtonText = secondaryButtonText;
            }

            return await dialog.ShowAsync();
        }
        finally
        {
            _dialogLock.Release();
        }
    }
}
