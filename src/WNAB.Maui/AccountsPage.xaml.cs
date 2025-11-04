using WNAB.MVM;

namespace WNAB.Maui;

public partial class AccountsPage : ContentPage
{
    private readonly AccountsViewModel _viewModel;
    private readonly IAuthenticationService _authService;
    
    public AccountsPage() : this(ServiceHelper.GetService<AccountsViewModel>(), ServiceHelper.GetService<IAuthenticationService>()) { }
    
    public AccountsPage(AccountsViewModel vm, IAuthenticationService authService)
    {
        InitializeComponent();
        _viewModel = vm;
        _authService = authService;
        BindingContext = vm;
    }

    // LLM-Dev: v1 Added automatic initialization when page appears
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

        await _viewModel.InitializeAsync();
    }
}