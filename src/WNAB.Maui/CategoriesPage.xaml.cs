namespace WNAB.Maui;

public partial class CategoriesPage : ContentPage
{
    private bool _loaded;

    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        if (BindingContext is CategoriesViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}
