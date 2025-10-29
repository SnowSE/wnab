using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class MainPage : ContentPage
{
    public MainPage() : this(ServiceHelper.GetService<PlanBudgetViewModel>()) { }
    public MainPage(PlanBudgetViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PlanBudgetViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}