using WNAB.MVM.Behaviors;

namespace WNAB.Maui;

// LLM-Dev: TransactionsPage code-behind - thin layer, delegates to ViewModel
public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;
    private readonly IAuthenticationService _authService;
  
    public TransactionsPage() : this(
        ServiceHelper.GetService<TransactionsViewModel>(), 
      ServiceHelper.GetService<IAuthenticationService>()) { }
    
    public TransactionsPage(
        TransactionsViewModel vm, 
        IAuthenticationService authService)
    {
        InitializeComponent();
        _viewModel = vm;
        _authService = authService;
        BindingContext = vm;
        
        System.Diagnostics.Debug.WriteLine("TransactionsPage: Constructor - BindingContext set");
        System.Diagnostics.Debug.WriteLine($"TransactionsPage: ViewModel.Model.Items.Count={vm.Model.Items.Count}");
    }

    // LLM-Dev: Added automatic initialization when page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        System.Diagnostics.Debug.WriteLine("TransactionsPage.OnAppearing: Starting");

        // Check authentication before loading page
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        System.Diagnostics.Debug.WriteLine($"TransactionsPage.OnAppearing: IsAuthenticated={isAuthenticated}");
        
        if (!isAuthenticated)
        {
            System.Diagnostics.Debug.WriteLine("TransactionsPage.OnAppearing: Not authenticated, navigating to Landing");
            await Shell.Current.GoToAsync("//Landing");
            return;
        }

        System.Diagnostics.Debug.WriteLine("TransactionsPage.OnAppearing: Calling ViewModel.InitializeAsync");
        await _viewModel.InitializeAsync();
        System.Diagnostics.Debug.WriteLine($"TransactionsPage.OnAppearing: Completed, Items.Count={_viewModel.Model.Items.Count}");
    }
}