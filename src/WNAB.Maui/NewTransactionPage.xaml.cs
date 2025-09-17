using Microsoft.Maui.Controls;

namespace WNAB.Maui
{
    public partial class NewTransactionPage : ContentPage
    {
        public NewTransactionPage()
        {
            InitializeComponent();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            // Navigate back to the main page
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
