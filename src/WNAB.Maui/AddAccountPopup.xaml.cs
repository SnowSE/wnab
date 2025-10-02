using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class AddAccountPopup : Popup
{
    public AddAccountPopup(AddAccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => Close();
        
        // LLM-Dev:v2 Initialize user session when popup opens to load saved user ID
        _ = Task.Run(async () => await vm.InitializeAsync());
    }
}