using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts boolean IsIncome value to a color (Green for income, Red for expense).
/// </summary>
public class IncomeExpenseColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncome)
  {
  return isIncome ? Colors.Green : Colors.Red;
        }
     return Colors.Red;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
    throw new NotImplementedException();
    }
}
