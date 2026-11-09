namespace MZikmund.UnoApp.WizardUi;

/// <summary>
/// A settings-style row: header and description on the left, the control as content on the right.
/// Stands in for the Community Toolkit's SettingsCard, which doesn't build against Uno.Sdk 7 yet.
/// The template lives in App.xaml.
/// </summary>
public sealed partial class SettingsRow : ContentControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsRow), new PropertyMetadata(""));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(object), typeof(SettingsRow), new PropertyMetadata(null));

    public SettingsRow()
    {
        DefaultStyleKey = typeof(SettingsRow);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
