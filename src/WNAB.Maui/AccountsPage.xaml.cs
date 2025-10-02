namespace WNAB.Maui;

public partial class AccountsPage : ContentPage
{
    private readonly AccountsViewModel _viewModel;
    
    public AccountsPage() : this(ServiceHelper.GetService<AccountsViewModel>()) { }
    public AccountsPage(AccountsViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    // LLM-Dev: v1 Added automatic initialization when page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}