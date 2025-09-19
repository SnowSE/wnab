using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace WNAB.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// DI registrations for MVVM
		builder.Services.AddSingleton<IPopupService, PopupService>();
		builder.Services.AddTransient<MainPageViewModel>();
		builder.Services.AddTransient<NewTransactionViewModel>();
		builder.Services.AddTransient<NewTransactionPopup>();
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
