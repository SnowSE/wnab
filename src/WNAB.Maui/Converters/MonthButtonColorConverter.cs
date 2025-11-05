using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converter to determine the background color of a month button based on whether it's selected.
/// </summary>
public class MonthButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentMonth && parameter is string monthStr && int.TryParse(monthStr, out int buttonMonth))
        {
            if (currentMonth == buttonMonth)
            {
                // Selected month - use primary blue color
                return Color.FromArgb("#007bff");
            }
            else
            {
                // Unselected month - use darker gray that works on dark background
                return Color.FromArgb("#495057");
            }
        }
        
        return Color.FromArgb("#495057");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
