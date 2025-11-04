using WNAB.MVM.Behaviors;

namespace WNAB.Maui;

// LLM-Dev: TransactionsPage code-behind following the same pattern as other pages
public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;
    private readonly IAuthenticationService _authService;
    
    public TransactionsPage() : this(ServiceHelper.GetService<TransactionsViewModel>(), ServiceHelper.GetService<IAuthenticationService>()) { }
    
    public TransactionsPage(TransactionsViewModel vm, IAuthenticationService authService)
    {
        InitializeComponent();
        _viewModel = vm;
        _authService = authService;
        BindingContext = vm;
    }

    // LLM-Dev: Added automatic initialization when page appears
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