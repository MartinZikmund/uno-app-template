using Microsoft.UI.Xaml.Markup;

namespace AppTemplate.Services.Localization;

/// <summary>
/// XAML markup extension for easy localization.
/// Usage: Text="{local:Localize Key=MyResourceKey}"
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    protected override object ProvideValue()
    {
        if (string.IsNullOrEmpty(Key))
        {
            return string.Empty;
        }

        return Localizer.Get(Key);
    }
}
