using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Logging;
using Kronos.RosterWizard.Application.Services;
using Kronos.RosterWizard.Infrastructure;
using Kronos.RosterWizard.Presentation.Pages;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IRosterRepository, JsonRosterRepository>();
		builder.Services.AddSingleton<IRosterGenerationService, RosterGenerationService>();
		builder.Services.AddSingleton<RosterDataTransferService>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<RosterCalendarViewModel>();
		builder.Services.AddTransient<StaffViewModel>();
		builder.Services.AddTransient<RulesViewModel>();
		builder.Services.AddTransient<RequestsViewModel>();
		builder.Services.AddTransient<DataToolsViewModel>();
		builder.Services.AddTransient<RosterReviewViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<RosterCalendarPage>();
		builder.Services.AddTransient<StaffPage>();
		builder.Services.AddTransient<RulesPage>();
		builder.Services.AddTransient<RequestsPage>();
		builder.Services.AddTransient<DataToolsPage>();
		builder.Services.AddTransient<RosterReviewPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
