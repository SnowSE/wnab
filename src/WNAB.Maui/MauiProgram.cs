using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

		// TODO: Fix service defaults conflict between WNAB.ServiceDefaults and WNAB.Maui.ServiceDefaults
		// Microsoft.Extensions.Hosting.Extensions.AddServiceDefaults(builder);

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

		// Register Budget logic services
		builder.Services.AddScoped<IBudgetService, BudgetService>();
		builder.Services.AddScoped<IBudgetSnapshotService, BudgetSnapshotService>(sp => new BudgetSnapshotService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
		builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
        builder.Services.AddScoped<ICategoryAllocationManagementService, CategoryAllocationManagementService>();
		builder.Services.AddScoped<ITransactionManagementService, TransactionManagementService>();

        // Authentication services
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
		builder.Services.AddSingleton<AuthenticationDelegatingHandler>();

		// DI registrations for MVVM
		builder.Services.AddSingleton<IMVMPopupService, PopupService>();
		builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        builder.Services.AddSingleton<MainPageModel>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();

		builder.Services.AddSingleton<LandingPage>();

		builder.Services.AddSingleton<AddTransactionModel>();
		builder.Services.AddSingleton<AddTransactionViewModel>();
		builder.Services.AddSingleton<TransactionPopup>();

        builder.Services.AddSingleton<EditTransactionModel>();
		builder.Services.AddSingleton<EditTransactionViewModel>();

		builder.Services.AddSingleton<EditTransactionSplitModel>();
		builder.Services.AddSingleton<EditTransactionSplitViewModel>();

		builder.Services.AddSingleton<AddSplitToTransactionModel>();
		builder.Services.AddSingleton<AddSplitToTransactionViewModel>();

		builder.Services.AddSingleton<AddCategoryModel>();
		builder.Services.AddSingleton<AddCategoryViewModel>();
		builder.Services.AddSingleton<AddCategoryPopup>();

		builder.Services.AddSingleton<EditCategoryModel>();
		builder.Services.AddSingleton<EditCategoryViewModel>();
		builder.Services.AddSingleton<EditCategoryPopup>();

		builder.Services.AddSingleton<AddAccountModel>();
		builder.Services.AddSingleton<AddAccountViewModel>();
		builder.Services.AddSingleton<AddAccountPopup>();

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