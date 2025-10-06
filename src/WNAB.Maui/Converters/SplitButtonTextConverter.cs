using System.Globalization;

namespace WNAB.Maui.Converters;

// LLM-Dev:v1 Converter to change button text based on split mode
public class SplitButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSplit)
            return isSplit ? "Single Category" : "Split Transaction";
        return "Split Transaction";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
