using Microsoft.UI.Xaml.Data;

namespace AppTemplate.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
	public bool Invert { get; set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		var boolValue = value is true;
		if (Invert)
		{
			boolValue = !boolValue;
		}

		return boolValue ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		var result = value is Visibility.Visible;
		return Invert ? !result : result;
	}
}
