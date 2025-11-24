using System.Globalization;

namespace WNAB.Maui.Converters;

public class IsEditingTransactionConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2)
            return false;

        if (values[0] is int transactionId && values[1] is int editingTransactionId)
        {
            return editingTransactionId == transactionId;
        }

        return false;
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
