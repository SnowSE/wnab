using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts an available amount to a color (green if positive/zero, red if negative).
/// Used for styling available amounts based on overspending.
/// </summary>
public class AvailableAmountColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal available)
        {
            return available >= 0 ? Colors.Green : Colors.Red;
        }

        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
