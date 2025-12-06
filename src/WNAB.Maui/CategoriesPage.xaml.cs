namespace WNAB.Maui;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;
    private readonly IAuthenticationService _authService;
    
    public CategoriesPage() : this(ServiceHelper.GetService<CategoriesViewModel>(), ServiceHelper.GetService<IAuthenticationService>()) { }
    
    public CategoriesPage(CategoriesViewModel vm, IAuthenticationService authService)
    {
        InitializeComponent();
        _viewModel = vm;
        _authService = authService;
        BindingContext = vm;
        
        System.Diagnostics.Debug.WriteLine("CategoriesPage: BindingContext set to CategoriesViewModel");
        System.Diagnostics.Debug.WriteLine($"CategoriesPage: AddCategoryModel is {(vm.AddCategoryModel != null ? "NOT NULL" : "NULL")}");

    }

    // LLM-Dev:v7 Added automatic initialization when page appears to refresh on navigation, following AccountsPage pattern
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

    /// <summary>
    /// Handle color selection in the inline add category form.
    /// </summary>
    private void OnColorTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string color)
        {
            _viewModel.AddCategoryModel.SelectedColor = color;
        }
    }
}
