using Microsoft.UI.Xaml.Data;

namespace AppTemplate.Converters;

public sealed class StringToVisibilityConverter : IValueConverter
{
	public bool Invert { get; set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		var shouldShow = Invert
			? string.IsNullOrEmpty(value as string)
			: !string.IsNullOrEmpty(value as string);
		return shouldShow ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException();
}
