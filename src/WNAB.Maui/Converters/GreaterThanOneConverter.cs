using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts an integer count to a boolean indicating if it's greater than 1.
/// Used to enable/disable the remove split button.
/// </summary>
public class GreaterThanOneConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
   if (value is int count)
      {
 return count > 1;
        }
    return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
        throw new NotImplementedException();
    }
}
