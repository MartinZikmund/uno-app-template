using AppTemplate.Services.Localization;
using Microsoft.UI.Xaml.Data;

namespace AppTemplate.Converters;

/// <summary>
/// Converts an enum value to a localized string using the Localizer.
/// The resource key format is: {EnumTypeName}_{EnumValueName}
/// </summary>
public class EnumToLocalizedStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var enumType = value.GetType();
        if (!enumType.IsEnum)
        {
            return value.ToString();
        }

        var enumName = Enum.GetName(enumType, value);
        if (enumName is null)
        {
            return value.ToString();
        }

        var resourceKey = $"{enumType.Name}_{enumName}";
        return Localizer.Get(resourceKey);
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
