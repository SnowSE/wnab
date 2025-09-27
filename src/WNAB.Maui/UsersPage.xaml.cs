namespace WNAB.Maui;

public partial class UsersPage : ContentPage
{
    public UsersPage(UsersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}