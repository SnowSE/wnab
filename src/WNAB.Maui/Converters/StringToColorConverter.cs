using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts a hex color string (e.g., "#ef4444") to a MAUI Color.
/// </summary>
public class StringToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrWhiteSpace(colorString))
        {
            return Color.FromArgb(colorString);
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
