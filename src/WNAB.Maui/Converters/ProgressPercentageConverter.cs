using System.Globalization;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts activity and budgeted amounts to a progress percentage (0-1).
/// Used for progress bar width calculations.
/// </summary>
public class ProgressPercentageConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not decimal activity || values[1] is not decimal budgeted)
            return 0.0;

        if (budgeted <= 0)
            return 0.0;

        var percentage = (double)Math.Abs(activity) / (double)budgeted;
        return Math.Min(percentage, 1.0); // Cap at 100%
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
