namespace WNAB.Maui;

public partial class CategoriesPage : ContentPage
{
    public CategoriesPage() : this(ServiceHelper.GetService<CategoriesViewModel>()) { }
    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
