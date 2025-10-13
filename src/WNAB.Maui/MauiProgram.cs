using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using WNAB.Services;

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

		builder.AddServiceDefaults();

		// Add configuration from appsettings.json
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("WNAB.Maui.appsettings.json");
		if (stream != null)
		{
			var config = new ConfigurationBuilder()
				.AddJsonStream(stream)
				.Build();
			builder.Configuration.AddConfiguration(config);
		}

		// Authentication services
		builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
		builder.Services.AddSingleton<AuthenticationDelegatingHandler>();

		// DI registrations for MVVM
		builder.Services.AddSingleton<IMVMPopupService, PopupService>();
		builder.Services.AddSingleton<MainPageModel>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();

		builder.Services.AddSingleton<TransactionModel>();
		builder.Services.AddSingleton<TransactionViewModel>();
		builder.Services.AddSingleton<TransactionPopup>();

        builder.Services.AddSingleton<AddCategoryModel>();
		builder.Services.AddSingleton<AddCategoryViewModel>();
		builder.Services.AddSingleton<AddCategoryPopup>();

		builder.Services.AddSingleton<AddUserModel>();
		builder.Services.AddSingleton<AddUserViewModel>();
		builder.Services.AddSingleton<AddUserPopup>();

		builder.Services.AddSingleton<AddAccountModel>();
		builder.Services.AddSingleton<AddAccountViewModel>();
		builder.Services.AddSingleton<AddAccountPopup>();

		builder.Services.AddSingleton<NewMainPageViewModel>();
		builder.Services.AddSingleton<NewMainPage>();



		// LLM-Dev:v2 Centralize base root for MAUI API calls with authentication
		var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7077/";
		builder.Services.AddHttpClient("wnab-api", client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
		})
		.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

		// Use the shared Logic services with the same named client
		builder.Services.AddSingleton(sp => new UserManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new AccountManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new CategoryManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new CategoryAllocationManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new TransactionManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));

		// ViewModels/Pages
		builder.Services.AddSingleton<CategoriesModel>();
		builder.Services.AddSingleton<CategoriesViewModel>();
		builder.Services.AddSingleton<CategoriesPage>();

		builder.Services.AddSingleton<UsersModel>();
		builder.Services.AddSingleton<UsersViewModel>();
		builder.Services.AddSingleton<UsersPage>();
        // LLM-Dev:v3 Register Accounts so Shell can resolve via DI (constructor requires VM)
        builder.Services.AddSingleton<AccountsModel>();
        builder.Services.AddSingleton<WNAB.MVM.AccountsViewModel>();
        builder.Services.AddSingleton<AccountsPage>();
		// LLM-Dev:v5 Re-add Login page & VM registrations (ensures Shell can resolve via DI)
		builder.Services.AddSingleton<LoginViewModel>();
		builder.Services.AddSingleton<LoginPage>();
		// LLM-Dev: Register Transactions page and view model
		builder.Services.AddSingleton<TransactionsModel>();
		builder.Services.AddSingleton<TransactionsViewModel>();
		builder.Services.AddSingleton<TransactionsPage>();
		// LLM-Dev: Register PlanBudget page, model, and view model
		builder.Services.AddSingleton<PlanBudgetModel>();
		builder.Services.AddSingleton<PlanBudgetViewModel>();
		builder.Services.AddSingleton<PlanBudgetPage>();

#if DEBUG
	builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		// LLM-Dev:v6 expose ServiceProvider for ServiceHelper, to support parameterless page constructors
		ServiceHelper.Services = app.Services;
		return app;
	}
}