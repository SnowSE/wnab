using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for TransactionsPage - uses inline editing instead of popups.
/// </summary>
public partial class TransactionsViewModel : ObservableObject
{
    private readonly AddTransactionViewModel _addTransactionViewModel;
    private readonly EditTransactionViewModel _editTransactionViewModel;
    private readonly EditTransactionSplitViewModel _editTransactionSplitViewModel;
    private readonly AddSplitToTransactionViewModel _addSplitToTransactionViewModel;

    public TransactionsModel Model { get; }
    public AddTransactionViewModel AddTransactionViewModel => _addTransactionViewModel;
    public EditTransactionViewModel EditTransactionViewModel => _editTransactionViewModel;
    public EditTransactionSplitViewModel EditTransactionSplitViewModel => _editTransactionSplitViewModel;
    public AddSplitToTransactionViewModel AddSplitToTransactionViewModel => _addSplitToTransactionViewModel;

    [ObservableProperty]
    private bool isAddFormVisible = false;

    [ObservableProperty]
    private bool isEditFormVisible = false;

    [ObservableProperty]
    private bool isEditSplitFormVisible = false;

    [ObservableProperty]
    private bool isAddSplitFormVisible = false;

    [ObservableProperty]
    private int? editingTransactionId = null;

    public TransactionsViewModel(
        TransactionsModel model, 
        AddTransactionViewModel addTransactionViewModel,
        EditTransactionViewModel editTransactionViewModel,
        EditTransactionSplitViewModel editTransactionSplitViewModel,
        AddSplitToTransactionViewModel addSplitToTransactionViewModel)
    {
        Model = model;
        _addTransactionViewModel = addTransactionViewModel;
        _editTransactionViewModel = editTransactionViewModel;
        _editTransactionSplitViewModel = editTransactionSplitViewModel;
        _addSplitToTransactionViewModel = addSplitToTransactionViewModel;
    }

    public async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("TransactionsViewModel.InitializeAsync: Starting");
        System.Diagnostics.Debug.WriteLine($"TransactionsViewModel.InitializeAsync: EditingTransactionId={EditingTransactionId}");
        await Model.InitializeAsync();
        System.Diagnostics.Debug.WriteLine($"TransactionsViewModel.InitializeAsync: Model initialized, Items.Count={Model.Items.Count}");
        await _addTransactionViewModel.InitializeAsync();
        await _editTransactionViewModel.InitializeAsync();
        await _editTransactionSplitViewModel.InitializeAsync();
        await _addSplitToTransactionViewModel.InitializeAsync();
        System.Diagnostics.Debug.WriteLine($"TransactionsViewModel.InitializeAsync: Completed, EditingTransactionId={EditingTransactionId}");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        System.Diagnostics.Debug.WriteLine("TransactionsViewModel.RefreshAsync: Called");
        await Model.RefreshAsync();
        System.Diagnostics.Debug.WriteLine($"TransactionsViewModel.RefreshAsync: Completed, Items.Count={Model.Items.Count}");
    }

    [RelayCommand]
    private void ToggleAddForm()
    {
        IsAddFormVisible = !IsAddFormVisible;
        
        if (!IsAddFormVisible)
        {
            _addTransactionViewModel.Model.Clear();
        }
    }

    [RelayCommand]
    private void CancelAddTransaction()
    {
        IsAddFormVisible = false;
        _addTransactionViewModel.Model.Clear();
    }

    [RelayCommand]
    private async Task SaveTransactionInline()
    {
        var (success, message) = await _addTransactionViewModel.Model.CreateTransactionAsync();
        
        if (success)
        {
            _addTransactionViewModel.Model.Clear();
            IsAddFormVisible = false;
            await Model.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteTransaction(int transactionId)
    {
        await Model.DeleteTransactionAsync(transactionId);
    }

    [RelayCommand]
    private async Task DeleteTransactionSplit(int splitId)
    {
        await Model.DeleteTransactionSplitAsync(splitId);
    }

    [RelayCommand]
    private async Task ModifyTransaction(int transactionId)
    {
        EditingTransactionId = transactionId;
        await _editTransactionViewModel.LoadTransactionAsync(transactionId);
    }

    [RelayCommand]
    private void CancelEditTransaction()
    {
        EditingTransactionId = null;
        _editTransactionViewModel.Model.Clear();
    }

    [RelayCommand]
    private async Task SaveEditTransaction()
    {
        var (success, message) = await _editTransactionViewModel.Model.UpdateTransactionAsync();

        if (success)
        {
            EditingTransactionId = null;
            _editTransactionViewModel.Model.Clear();
            await Model.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task ModifyTransactionSplit(int splitId)
    {
        IsEditSplitFormVisible = true;
        await _editTransactionSplitViewModel.LoadSplitAsync(splitId);
    }

    [RelayCommand]
    private void CancelEditTransactionSplit()
    {
        IsEditSplitFormVisible = false;
        _editTransactionSplitViewModel.Model.Clear();
    }

    [RelayCommand]
    private async Task SaveEditTransactionSplit()
    {
        var (success, message) = await _editTransactionSplitViewModel.Model.UpdateSplitAsync();

        if (success)
        {
            _editTransactionSplitViewModel.Model.Clear();
            IsEditSplitFormVisible = false;
            await Model.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task AddSplitToTransaction(int transactionId)
    {
        IsAddSplitFormVisible = true;
        await _addSplitToTransactionViewModel.LoadTransactionAsync(transactionId);
    }

    [RelayCommand]
    private void CancelAddSplitToTransaction()
    {
        IsAddSplitFormVisible = false;
        _addSplitToTransactionViewModel.Model.Clear();
    }

    [RelayCommand]
    private async Task SaveAddSplitToTransaction()
    {
        var (success, message) = await _addSplitToTransactionViewModel.Model.CreateSplitAsync();

        if (success)
        {
            _addSplitToTransactionViewModel.Model.Clear();
            IsAddSplitFormVisible = false;
            await Model.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
