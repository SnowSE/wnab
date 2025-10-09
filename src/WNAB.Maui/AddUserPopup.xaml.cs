using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class AddUserPopup : Popup
{
    public AddUserPopup(AddUserViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += async (_, _) => await CloseAsync();
    }
}