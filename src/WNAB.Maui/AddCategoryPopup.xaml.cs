using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class AddCategoryPopup : Popup
{
    public AddCategoryPopup(AddCategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.RequestClose += (_, _) => Close();
    }
}