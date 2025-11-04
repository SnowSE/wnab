using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class MainPage : ContentPage
{
    private readonly IAuthenticationService _authService;

    public MainPage() : this(ServiceHelper.GetService<PlanBudgetViewModel>(), ServiceHelper.GetService<IAuthenticationService>()) { }

    public MainPage(PlanBudgetViewModel vm, IAuthenticationService authService)
    {
        InitializeComponent();
        BindingContext = vm;
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check authentication before loading page
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            await Shell.Current.GoToAsync("//Landing");
            return;
        }

        if (BindingContext is PlanBudgetViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}