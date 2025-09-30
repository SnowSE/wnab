namespace WNAB.Maui;

public partial class AccountsPage : ContentPage
{
    public AccountsPage() : this(ServiceHelper.GetService<AccountsViewModel>()) { }
    public AccountsPage(AccountsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}