using AppTemplate.Core.Navigation;
using AppTemplate.Core.ViewModels;

namespace AppTemplate.Views;

public partial class SettingsViewBase : ViewBase<SettingsViewModel> { }

[NavigationInfo(NavigationSection.Settings)]
public sealed partial class SettingsView : SettingsViewBase
{
    public SettingsView()
    {
        this.InitializeComponent();
    }

    // Sample usage of ColorPickerDialog via IDialogService (see AppTemplate.Dialogs.ColorPickerDialog):
    //
    //     var dialog = new ColorPickerDialog(Colors.SteelBlue);
    //     var result = await _dialogService.ShowAsync(dialog);
    //     if (result == ContentDialogResult.Primary)
    //     {
    //         var chosenColor = dialog.SelectedColor;
    //         // Apply the chosen color...
    //     }
    //
    // _dialogService is an IDialogService resolved via constructor injection.
}
