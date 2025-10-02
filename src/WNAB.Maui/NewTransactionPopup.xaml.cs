using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class NewTransactionPopup : Popup
{
    public NewTransactionPopup(NewTransactionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += async (_, _) => await CloseAsync();
    }
}
