using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
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
		builder.Services.AddTransient<AddCategoryViewModel>();
		builder.Services.AddTransient<AddCategoryPopup>();
		builder.Services.AddTransient<MainPage>();

		// LLM-Dev: Categories feature DI wiring (hardcoded base URL)
		builder.Services.AddSingleton<ICategoriesService>(sp =>
		{
			var http = new HttpClient { BaseAddress = new Uri("https://localhost:7077/") };
			return new CategoriesService(http);
		});

		// LLM-Dev: Account management service registration (funnel writes via API)
		builder.Services.AddSingleton(sp =>
		{
			var http = new HttpClient { BaseAddress = new Uri("https://localhost:7077/") };
			return new WNAB.Logic.AccountManagementService(http);
		});

		// LLM-Dev: User management service registration (POST to /users)
		builder.Services.AddSingleton(sp =>
		{
			var http = new HttpClient { BaseAddress = new Uri("https://localhost:7077/") };
			return new WNAB.Logic.UserManagementService(http);
		});
		builder.Services.AddTransient<CategoriesViewModel>();
		builder.Services.AddTransient<CategoriesPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
