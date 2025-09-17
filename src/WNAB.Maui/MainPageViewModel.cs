using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace WNAB.Maui
{
    public class MainPageViewModel
    {
        public ICommand NavigateToNewTransactionCommand { get; }

        public MainPageViewModel()
        {
            NavigateToNewTransactionCommand = new Command(async () =>
            {
                // Use Shell navigation if using Shell
                await Shell.Current.GoToAsync(nameof(NewTransactionPage));
            });
        }
    }
}
