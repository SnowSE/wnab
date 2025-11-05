using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts boolean balanced state to color (Green if balanced, Red if not).
/// </summary>
public class BalanceColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isBalanced)
        {
            return isBalanced ? Colors.Green : Colors.Red;
        }
        return Colors.Red;
  }

public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
    throw new NotImplementedException();
    }
}
