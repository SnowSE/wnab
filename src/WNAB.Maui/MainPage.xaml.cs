using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}