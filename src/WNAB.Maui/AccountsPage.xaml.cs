namespace WNAB.Maui;

public partial class AccountsPage : ContentPage
{
    public AccountsPage(AccountsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}