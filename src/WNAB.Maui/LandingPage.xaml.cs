namespace WNAB.Maui;

public partial class LandingPage : ContentPage
{
    private readonly IAuthenticationService _authService;

    public LandingPage() : this(ServiceHelper.GetService<IAuthenticationService>())
    {
  }

    public LandingPage(IAuthenticationService authService)
    {
      InitializeComponent();
  _authService = authService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
   var success = await _authService.LoginAsync();
        if (success)
      {
            // Navigate to main app
         await Shell.Current.GoToAsync("//MainPage");
        }
        else
        {
            await DisplayAlert("Login Failed", "Unable to authenticate. Please try again.", "OK");
        }
    }
}
