namespace WNAB.Maui;

public partial class UsersPage : ContentPage
{
    private readonly UsersViewModel _viewModel;
    
    public UsersPage() : this(ServiceHelper.GetService<UsersViewModel>()) { }
    public UsersPage(UsersViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    // LLM-Dev:v5 Updated to call InitializeAsync following refactored ViewModel pattern
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}