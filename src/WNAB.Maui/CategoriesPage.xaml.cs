namespace WNAB.Maui;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;
    
    public CategoriesPage() : this(ServiceHelper.GetService<CategoriesViewModel>()) { }
    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    // LLM-Dev:v7 Added automatic initialization when page appears to refresh on navigation, following AccountsPage pattern
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
