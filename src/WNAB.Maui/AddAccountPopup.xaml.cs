namespace WNAB.Maui;

public partial class AddAccountPopup : Popup
{
    public AddAccountPopup(AddAccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => CloseAsync();
        
        // LLM-Dev:v3 Initialize user session when popup opens to load saved user ID (internal only)
        _ = Task.Run(async () => await vm.InitializeAsync());
    }
}