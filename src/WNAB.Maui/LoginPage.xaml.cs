namespace WNAB.Maui;

public partial class LoginPage : ContentPage
{
    public LoginPage() : this(ServiceHelper.GetService<LoginViewModel>()) { }
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
