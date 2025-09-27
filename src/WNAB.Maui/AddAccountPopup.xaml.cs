using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class AddAccountPopup : Popup
{
    public AddAccountPopup(AddAccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => Close();
    }
}