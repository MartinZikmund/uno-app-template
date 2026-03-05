namespace AppTemplate.Services.Dialogs;

public interface IDialogCoordinator
{
	Task<ContentDialogResult> ShowAsync(ContentDialog dialog);
}
