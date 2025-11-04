using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class MonthYearPickerPopup : Popup
{
    private readonly PlanBudgetViewModel _viewModel;

    public MonthYearPickerPopup(PlanBudgetViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnMonthSelected(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string monthStr)
        {
            if (int.TryParse(monthStr, out int month))
            {
                await _viewModel.SelectMonthCommand.ExecuteAsync(month);
                await CloseAsync();
            }
        }
    }
}
