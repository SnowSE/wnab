using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converter that returns a black border color if the value matches the parameter, otherwise transparent.
/// Used to show which color is selected in the color picker.
/// </summary>
public class ColorMatchToBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedColor && parameter is string colorToCheck)
        {
            return string.Equals(selectedColor, colorToCheck, StringComparison.OrdinalIgnoreCase)
                ? Colors.Black
                : Colors.Transparent;
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
