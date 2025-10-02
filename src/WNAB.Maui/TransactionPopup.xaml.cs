using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class TransactionPopup : Popup
{
    public TransactionPopup(TransactionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => Close();
    }
}
