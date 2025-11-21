using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts an available amount to a boolean indicating overspending.
/// Returns true if available is negative (overspent), false otherwise.
/// </summary>
public class IsOverspentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal available)
        {
            return available < 0;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
