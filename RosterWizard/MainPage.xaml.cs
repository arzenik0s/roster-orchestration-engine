using Kronos.RosterWizard.Presentation.Pages;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        var navigation = new VerticalStackLayout
        {
            Children =
            {
                NavButton("Roster calendar", "calendar", "calendar-content"),
                NavButton("Roster review", "review", "review-content"),
                NavButton("Staff", "staff", "staff-content"),
                NavButton("Rules & patterns", "rules", "rules-content"),
                NavButton("Requests & leave", "requests", "requests-content"),
                NavButton("Data import / export", "data", "data-content")
            }
        };

        var details = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                UiKit.Heading("Roster overview?", 17),
                new Label { Text = "{Binding Summary}", FontSize = 18, TextColor = UiKit.Ink }
            }
        };
        ((Label)details.Children[1]).SetBinding(Label.TextProperty, nameof(MainViewModel.Summary));

        var generateCard = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                UiKit.Heading("Generate a roster", 17),
                UiKit.Caption("Choose the first date. The roster length comes from the configured roster-day pattern."),
                new DatePicker { Date = DateTime.Today },
                UiKit.Action("Generate optimized roster", viewModel.GenerateCommand),
                new Label { TextColor = UiKit.Accent, FontSize = 13 }
            }
        };
        ((DatePicker)generateCard.Children[2]).SetBinding(DatePicker.DateProperty, nameof(MainViewModel.StartDate));
        ((Label)generateCard.Children[4]).SetBinding(Label.TextProperty, nameof(MainViewModel.StatusMessage));

        Content = new ScrollView
        {
            BackgroundColor = UiKit.Canvas,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(28),
                Spacing = 22,
                Children =
                {
                    UiKit.Heading("KronOS | ShiftSync", 30),
                    UiKit.Caption("Fair, rule-aware nursing rosters — managed locally on this device."),
                    UiKit.Card(details),
                    UiKit.Card(generateCard),
                    UiKit.Heading("Workspace", 18),
                    navigation
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private static Button NavButton(string title, string itemRoute, string contentRoute)
    {
        var button = UiKit.Secondary(title);
        button.Margin = new Thickness(0, 0, 10, 10);
        button.Clicked += async (_, _) => await Shell.Current.GoToAsync($"//{itemRoute}/{contentRoute}");
        return button;
    }
}
