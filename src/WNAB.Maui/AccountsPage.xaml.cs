using WNAB.MVM;

namespace WNAB.Maui;

public partial class AccountsPage : ContentPage
{
    private readonly AccountsViewModel _viewModel;
    private readonly IAuthenticationService _authService;
    private const double CardTargetWidth = 320; // px
    private const double ItemSpacing = 16; // matches XAML spacing
    
    public AccountsPage() : this(ServiceHelper.GetService<AccountsViewModel>(), ServiceHelper.GetService<IAuthenticationService>()) { }
    
    public AccountsPage(AccountsViewModel vm, IAuthenticationService authService)
    {
        InitializeComponent();
        _viewModel = vm;
        _authService = authService;
        BindingContext = vm;

        // Keep the grid responsive as the window resizes
        SizeChanged += (_, __) => UpdateGridSpan();
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

        // Ensure correct initial span once data and layout are ready
        UpdateGridSpan();
    }

    private void UpdateGridSpan()
    {
        if (AccountsCollection?.ItemsLayout is GridItemsLayout grid)
        {
            // Use the actual width of the collection to compute an appropriate number of columns
            var width = AccountsCollection.Width;
            if (width <= 0)
                return;

            var span = Math.Max(1, (int)Math.Floor((width + ItemSpacing) / (CardTargetWidth + ItemSpacing)));
            if (grid.Span != span)
            {
                grid.Span = span;
            }
        }
    }
}