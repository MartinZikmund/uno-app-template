namespace AppTemplate.Resources;

/// <summary>
/// Centralizes app-wide <see cref="Microsoft.UI.Xaml.DataTemplate" /> definitions so individual
/// views don't have to redeclare them. Merged into <c>App.xaml</c>'s <c>MergedDictionaries</c>.
/// </summary>
public sealed partial class DataTemplates : ResourceDictionary
{
    public DataTemplates()
    {
        InitializeComponent();
    }
}
