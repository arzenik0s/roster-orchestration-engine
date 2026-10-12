using Kronos.RosterWizard.Presentation.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Kronos.RosterWizard;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        BackgroundColor = Color.FromArgb("#F4F7F6");
        FlyoutBackgroundColor = Colors.White;
        FlyoutHeader = new VerticalStackLayout
        {
            Padding = new Thickness(22, 24),
            Children =
            {
                new Label { Text = "KronOS", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#203532") },
                new Label { Text = "Roster manager", FontSize = 13, TextColor = Color.FromArgb("#71817E") }
            }
        };

        AddPage(services, "Dashboard", "dashboard", "dashboard-content", typeof(MainPage));
        AddPage(services, "Roster calendar", "calendar", "calendar-content", typeof(RosterCalendarPage));
        AddPage(services, "Roster review", "review", "review-content", typeof(RosterReviewPage));
        AddPage(services, "Staff", "staff", "staff-content", typeof(StaffPage));
        AddPage(services, "Rules & patterns", "rules", "rules-content", typeof(RulesPage));
        AddPage(services, "Requests & leave", "requests", "requests-content", typeof(RequestsPage));
        AddPage(services, "Data import / export", "data", "data-content", typeof(DataToolsPage));
    }

    private void AddPage(IServiceProvider services, string title, string route, string contentRoute, Type pageType)
    {
        var item = new FlyoutItem { Title = title, Route = route };
        item.Items.Add(new ShellContent
        {
            Title = title,
            Route = contentRoute,
            ContentTemplate = new DataTemplate(() => services.GetRequiredService(pageType))
        });
        Items.Add(item);
    }
}
