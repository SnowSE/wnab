using WNAB.Logic.Data;

namespace WNAB.Logic.ViewModels;

//todo: move this into logic somewhere and actually make it a vm
public class TransactionEntryViewModel
{
    public DateTime Date { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Payee { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public ICollection<TransactionSplit> Splits { get; set; } = new List<TransactionSplit>();
}