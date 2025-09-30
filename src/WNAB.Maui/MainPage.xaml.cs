using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MainPageViewModel vm && vm.RefreshUserIdCommand.CanExecute(null))
        {
            await vm.RefreshUserIdCommand.ExecuteAsync(null);
        }
    }
}