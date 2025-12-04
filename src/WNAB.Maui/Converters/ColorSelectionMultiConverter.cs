using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// MultiBinding converter that returns a black border color if the two color values match, otherwise transparent.
/// Used for color picker selection highlighting with dynamically bound colors.
/// Values[0] = the color option being rendered (from ItemsSource)
/// Values[1] = the currently selected color (EditColor)
/// </summary>
public class ColorSelectionMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is string colorOption && values[1] is string selectedColor)
        {
            return string.Equals(colorOption, selectedColor, StringComparison.OrdinalIgnoreCase)
                ? Colors.Black
                : Colors.Transparent;
        }

        return Colors.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
