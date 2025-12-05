using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
				fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
			});

		// Add configuration from appsettings.json
		var assembly = Assembly.GetExecutingAssembly();
		var configBuilder = new ConfigurationBuilder();
		
		// Load base appsettings.json
		using var baseStream = assembly.GetManifestResourceStream("WNAB.Maui.appsettings.json");
		if (baseStream != null)
		{
			configBuilder.AddJsonStream(baseStream);
		}

#if !DEBUG
		// Load production settings in Release mode (overrides base settings)
		using var prodStream = assembly.GetManifestResourceStream("WNAB.Maui.appsettings.Production.json");
		if (prodStream != null)
		{
			configBuilder.AddJsonStream(prodStream);
		}
#endif

		var config = configBuilder.Build();
		builder.Configuration.AddConfiguration(config);

		// Register Budget logic services
		builder.Services.AddScoped<IBudgetService, BudgetService>();
		builder.Services.AddScoped<IBudgetSnapshotService, BudgetSnapshotService>(sp => new BudgetSnapshotService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
        builder.Services.AddScoped<ICategoryAllocationManagementService, CategoryAllocationManagementService>();
		builder.Services.AddScoped<ITransactionManagementService, TransactionManagementService>();

        // Authentication services
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
		builder.Services.AddSingleton<AuthenticationDelegatingHandler>();

		// Alert service for MAUI
		builder.Services.AddSingleton<IAlertService, MauiAlertService>();
		
		// Main page
        builder.Services.AddSingleton<MainPageModel>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();

		builder.Services.AddSingleton<LandingPage>();

		// Transaction ViewModels (inline editing)
		builder.Services.AddSingleton<AddTransactionModel>();
		builder.Services.AddSingleton<AddTransactionViewModel>();

        builder.Services.AddSingleton<EditTransactionModel>();
		builder.Services.AddSingleton<EditTransactionViewModel>();

		builder.Services.AddSingleton<EditTransactionSplitModel>();
		builder.Services.AddSingleton<EditTransactionSplitViewModel>();

		builder.Services.AddSingleton<AddSplitToTransactionModel>();
		builder.Services.AddSingleton<AddSplitToTransactionViewModel>();

		// Category Models (inline editing)
		builder.Services.AddSingleton<AddCategoryModel>();

		// Account Models (inline editing)
		builder.Services.AddSingleton<AddAccountModel>();

		var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7077/";
		builder.Services.AddHttpClient("wnab-api", client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
		})
		.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

		// Use the shared Logic services with the same named client
		builder.Services.AddSingleton(sp => new AccountManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new CategoryManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new CategoryAllocationManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddSingleton(sp => new TransactionManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));

		// ViewModels/Pages
		builder.Services.AddSingleton<CategoriesModel>();
		builder.Services.AddSingleton<CategoriesViewModel>();
		builder.Services.AddSingleton<CategoriesPage>();

        builder.Services.AddSingleton<AccountsModel>();
		builder.Services.AddSingleton<WNAB.MVM.AccountsViewModel>();
        builder.Services.AddSingleton<AccountsPage>();

		builder.Services.AddSingleton<TransactionsModel>();
		builder.Services.AddSingleton<TransactionsViewModel>();
		builder.Services.AddSingleton<TransactionsPage>();

		builder.Services.AddSingleton<PlanBudgetModel>();
		builder.Services.AddSingleton<PlanBudgetViewModel>();

#if DEBUG
	builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		ServiceHelper.Services = app.Services;
		return app;
	}
}