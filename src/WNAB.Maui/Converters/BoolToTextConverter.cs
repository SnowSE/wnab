using System.Globalization;

namespace WNAB.Maui.Converters;

public class BoolToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = string.Empty;
    public string FalseText { get; set; } = string.Empty;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueText : FalseText;
        }
        return FalseText;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
