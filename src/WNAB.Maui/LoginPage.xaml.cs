using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class LoginPage : Popup
{
    public LoginPage() : this(ServiceHelper.GetService<LoginViewModel>()) { }
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => CloseAsync();
    }
}
