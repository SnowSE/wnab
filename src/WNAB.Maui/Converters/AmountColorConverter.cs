using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts decimal amount to a color (Green for positive, Red for negative).
/// </summary>
public class AmountColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal amount)
   {
 return amount >= 0 ? Colors.Green : Colors.Red;
  }
        return Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
