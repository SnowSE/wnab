using System.Collections;
using System.Globalization;

namespace WNAB.Maui.Converters;

public class TotalBalanceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable enumerable)
        {
            decimal total = 0;
            foreach (var item in enumerable)
            {
                if (item is WNAB.Data.Account account)
                {
                    total += account.CachedBalance;
                }
            }
            return total.ToString("C", culture);
        }
        return "$0.00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
