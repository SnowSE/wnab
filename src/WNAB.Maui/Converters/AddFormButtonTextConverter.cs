using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts form visibility boolean to button text ("Cancel" if visible, "New Transaction" if not).
/// </summary>
public class AddFormButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is bool isVisible)
        {
         return isVisible ? "Cancel" : "New Transaction";
        }
        return "New Transaction";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
