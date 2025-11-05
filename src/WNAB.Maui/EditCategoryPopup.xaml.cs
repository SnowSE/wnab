using CommunityToolkit.Maui.Views;
using WNAB.MVM;

namespace WNAB.Maui;

public partial class EditCategoryPopup : Popup
{
    private readonly EditCategoryViewModel _viewModel;

    public EditCategoryPopup(EditCategoryViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
        vm.RequestClose += OnRequestCloseAsync;
    }

    public void Initialize(int id, string name, string? color, bool isActive)
    {
        _viewModel.Initialize(id, name, color, isActive);
    }

    private async void OnRequestCloseAsync(object? sender, EventArgs e)
    {
        try
        {
            await CloseAsync();
        }
        catch (InvalidOperationException)
        {
            // do nothing, popup is already closed
        }
    }
}
