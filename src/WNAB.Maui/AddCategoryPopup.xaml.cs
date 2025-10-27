using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class AddCategoryPopup : Popup
{
    public AddCategoryPopup(AddCategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += OnRequestCloseAsync;
    }

    private async void OnRequestCloseAsync(object? sender, EventArgs e)
    {
        try
        {
            await CloseAsync();
        }
        catch (InvalidOperationException)
        {
            // do nothing , popup is already closed
        }
    }
}