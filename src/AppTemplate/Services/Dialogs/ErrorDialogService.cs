using AppTemplate.Services.Logging;

namespace AppTemplate.Services.Dialogs;

public sealed class ErrorDialogService(
    IDialogService dialogService,
    IFolderLauncher folderLauncher,
    ILogFolderProvider logFolder,
    IStringLocalizer localizer,
    ILogger<ErrorDialogService> logger) : IErrorDialogService
{
    public async Task ShowAsync(string title, string message, Exception? exception = null)
    {
        try
        {
            // Mobile and web users can't reach the folder, so don't send them looking for it.
            var logHint = folderLauncher.IsSupported
                ? localizer["ErrorDialogLogHintFormat", logFolder.FolderPath].Value
                : null;

            ContentDialog dialog = new()
            {
                Title = title,
                Content = new TextBlock
                {
                    Text = ErrorMessage.Compose(message, exception, logHint),
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                },
                CloseButtonText = localizer["Close"],
                DefaultButton = ContentDialogButton.Close,
            };

            if (folderLauncher.IsSupported)
            {
                dialog.SecondaryButtonText = localizer["OpenLogsFolder"];
                dialog.SecondaryButtonClick += OnOpenLogsFolderClick;
            }

            await dialogService.ShowAsync(dialog);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not show the error dialog for: {Message}", message);
        }
    }

    private async void OnOpenLogsFolderClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            // Keep the dialog open: its message is what the user will be asked to quote.
            args.Cancel = true;
            await folderLauncher.OpenAsync(logFolder.FolderPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not open the logs folder from the error dialog");
        }
    }
}
