using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts boolean IsIncome value to "Income" or "Expense" text.
/// </summary>
public class IncomeExpenseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncome)
        {
            return isIncome ? "Income" : "Expense";
        }
        return "Expense";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
