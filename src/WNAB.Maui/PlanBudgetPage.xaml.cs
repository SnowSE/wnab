namespace WNAB.Maui;

// LLM-Dev: Code-behind for PlanBudgetPage. Follows MVVM by delegating logic to PlanBudgetViewModel
public partial class PlanBudgetPage : ContentPage
{
    private readonly PlanBudgetViewModel _viewModel;

    public PlanBudgetPage(PlanBudgetViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    // LLM-Dev: Initialize the ViewModel when the page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
