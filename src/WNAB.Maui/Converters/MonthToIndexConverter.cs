using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts between month number (1-12) and picker index (0-11).
/// </summary>
public class MonthToIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int month && month >= 1 && month <= 12)
        {
            return month - 1; // Month 1 (January) -> Index 0
        }
        return 0; // Default to January
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0 && index <= 11)
        {
            return index + 1; // Index 0 -> Month 1 (January)
        }
        return 1; // Default to January
    }
}
