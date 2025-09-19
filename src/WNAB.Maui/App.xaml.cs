namespace WNAB.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Shell will resolve pages; MainPage constructor uses DI
		return new Window(new AppShell());
	}
}