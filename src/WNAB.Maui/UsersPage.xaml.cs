namespace WNAB.Maui;

public partial class UsersPage : ContentPage
{
    public UsersPage() : this(ServiceHelper.GetService<UsersViewModel>()) { }
    public UsersPage(UsersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}