using System.Globalization;
using WNAB.Data;

namespace WNAB.Maui.Converters;

/// <summary>
/// Converts a CategoryAllocation and BudgetSnapshot to a progress bar percentage (0-1).
/// </summary>
public class AllocationProgressConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return 0.0;
            
        if (values[0] is not CategoryAllocation allocation)
            return 0.0;
            
        if (values[1] is not BudgetSnapshot snapshot)
            return 0.0;

        if (allocation.BudgetedAmount <= 0)
            return 0.0;

        var categorySnapshot = snapshot.Categories
            .FirstOrDefault(c => c.CategoryId == allocation.CategoryId);
            
        if (categorySnapshot == null)
            return 0.0;

        var percentage = (double)Math.Abs(categorySnapshot.Activity) / (double)allocation.BudgetedAmount;
        return Math.Min(percentage, 1.0); // Cap at 100%
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
