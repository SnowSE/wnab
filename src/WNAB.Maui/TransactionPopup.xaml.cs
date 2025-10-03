using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

// LLM-Dev:v2 Initialize ViewModel on popup creation
public partial class TransactionPopup : Popup
{
    public TransactionPopup(TransactionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => Close();
        
        // LLM-Dev:v2 Initialize the ViewModel to load accounts and categories
        _ = vm.InitializeAsync();
    }
}
