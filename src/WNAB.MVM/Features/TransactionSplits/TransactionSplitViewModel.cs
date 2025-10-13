using CommunityToolkit.Mvvm.ComponentModel;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for individual transaction split items - thin coordination layer between View and Model.
/// Delegates all business logic to TransactionSplitModel.
/// </summary>
public partial class TransactionSplitViewModel : ObservableObject
{
    public TransactionSplitModel Model { get; }

    public TransactionSplitViewModel(TransactionSplitModel model)
    {
        Model = model;
    }

    /// <summary>
    /// Convenience constructor that creates a new Model instance.
    /// </summary>
    public TransactionSplitViewModel() : this(new TransactionSplitModel())
    {
    }
}
