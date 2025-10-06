using WNAB.Maui.Behaviors;

namespace WNAB.Maui;

// LLM-Dev: TransactionsPage code-behind following the same pattern as other pages
public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;
    
    public TransactionsPage() : this(ServiceHelper.GetService<TransactionsViewModel>()) { }
    public TransactionsPage(TransactionsViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    // LLM-Dev: Added automatic initialization when page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}