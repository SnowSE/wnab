using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using CommunityToolkit.Maui;
using WNAB.Logic; // LLM-Dev: Use shared Logic services in MAUI too

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
		builder.Services.AddTransient<TransactionViewModel>();
		builder.Services.AddTransient<TransactionPopup>();
		builder.Services.AddTransient<AddCategoryViewModel>();
		builder.Services.AddTransient<AddCategoryPopup>();
		builder.Services.AddTransient<AddUserViewModel>();
		builder.Services.AddTransient<AddUserPopup>();
		builder.Services.AddTransient<AddAccountViewModel>();
		builder.Services.AddTransient<AddAccountPopup>();
		builder.Services.AddTransient<MainPage>();

		// LLM-Dev:v2 Centralize base root for MAUI API calls. Using a single named HttpClient helps keep base consistent.
		builder.Services.AddHttpClient("wnab-api", client =>
		{
			// You can switch this to Aspire discovery in AppHost or read from config.
			client.BaseAddress = new Uri("https://localhost:7077/");
		});

		// Use the shared Logic services with the same named client
		builder.Services.AddSingleton(sp => new UserManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new AccountManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new CategoryManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new CategoryAllocationManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new TransactionManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));

		// ViewModels/Pages
		builder.Services.AddTransient<CategoriesViewModel>();
		builder.Services.AddTransient<CategoriesPage>();
		builder.Services.AddTransient<UsersViewModel>();
		builder.Services.AddTransient<UsersPage>();
        // LLM-Dev:v3 Register Accounts so Shell can resolve via DI (constructor requires VM)
        builder.Services.AddTransient<AccountsViewModel>();
        builder.Services.AddTransient<AccountsPage>();
		// LLM-Dev:v5 Re-add Login page & VM registrations (ensures Shell can resolve via DI)
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<LoginPage>();
		// LLM-Dev: Register Transactions page and view model
		builder.Services.AddTransient<TransactionsViewModel>();
		builder.Services.AddTransient<TransactionsPage>();
		// LLM-Dev: Register PlanBudget page and view model
		builder.Services.AddTransient<PlanBudgetViewModel>();
		builder.Services.AddTransient<PlanBudgetPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		// LLM-Dev:v6 expose ServiceProvider for ServiceHelper, to support parameterless page constructors
		ServiceHelper.Services = app.Services;
		return app;
	}
}
