using System.Globalization;

namespace WNAB.Maui.Converters;

public class IsNotEditingTransactionConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2)
            return true; // Default to visible if converter fails

        if (values[0] is int transactionId)
        {
            // Check if values[1] is either null or an int that doesn't match transactionId
            if (values[1] is null)
                return true; // Not editing, show read-only view
            
            if (values[1] is int editingTransactionId)
            {
                // Show read-only view when NOT editing this transaction
                return editingTransactionId != transactionId;
            }
        }

        return true; // Default to visible
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
