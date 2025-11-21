using System.Globalization;
using WNAB.Data;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts a CategoryAllocation to a progress bar width based on activity vs budgeted amount.
/// Returns a value between 0 and parent width.
/// </summary>
public class AllocationProgressWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CategoryAllocation allocation)
            return 0.0;

        if (allocation.BudgetedAmount <= 0)
            return 0.0;

        // TODO: Get activity from snapshot instead
        // For now, return 0 until we wire up snapshot data
        return 0.0;
        
        // var percentage = (double)Math.Abs(allocation.Activity) / (double)allocation.BudgetedAmount;
        // var cappedPercentage = Math.Min(percentage, 1.0); // Cap at 100%

        // If parameter is provided, it should be the parent width
        // Otherwise, return the percentage for use with RelativeLayout
        // if (parameter is double parentWidth)
        // {
        //     return cappedPercentage * parentWidth;
        // }

        // Return as percentage (0-1) for WidthRequest with RelativeToParent
        // return cappedPercentage;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
