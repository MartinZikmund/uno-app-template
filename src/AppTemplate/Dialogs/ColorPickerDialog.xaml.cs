using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace AppTemplate.Dialogs;

public sealed partial class ColorPickerDialog : ContentDialog
{
    public ColorPickerDialog() : this(Colors.Transparent)
    {
    }

    public ColorPickerDialog(Color initialColor)
    {
        this.InitializeComponent();
        ColorPickerControl.Color = initialColor;
    }

    /// <summary>
    /// Gets the color currently selected in the <see cref="ColorPicker" />.
    /// </summary>
    public Color SelectedColor => ColorPickerControl.Color;
}
