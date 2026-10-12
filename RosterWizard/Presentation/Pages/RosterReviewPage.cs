using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Pages;

public sealed class RosterReviewPage : ContentPage
{
    private readonly RosterReviewViewModel _viewModel;

    public RosterReviewPage(RosterReviewViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Roster review";
        BackgroundColor = UiKit.Canvas;

        var staffList = new CollectionView
        {
            HeightRequest = 420,
            ItemTemplate = new DataTemplate(() =>
            {
                var name = new Label { FontAttributes = FontAttributes.Bold, TextColor = UiKit.Ink };
                name.SetBinding(Label.TextProperty, nameof(StaffRosterReview.Name));
                var email = new Label { FontSize = 12, TextColor = UiKit.Muted };
                email.SetBinding(Label.TextProperty, nameof(StaffRosterReview.Email));
                var count = new Label { FontAttributes = FontAttributes.Bold, TextColor = UiKit.Accent };
                count.SetBinding(Label.TextProperty, nameof(StaffRosterReview.ShiftCount), stringFormat: "{0} shifts");
                var schedule = new Label { FontSize = 13, TextColor = UiKit.Ink };
                schedule.SetBinding(Label.TextProperty, nameof(StaffRosterReview.Assignments));
                return UiKit.Card(new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { name, email, count, schedule }
                });
            })
        };
        staffList.SetBinding(ItemsView.ItemsSourceProperty, nameof(RosterReviewViewModel.StaffSchedules));

        var requestsList = new CollectionView
        {
            HeightRequest = 340,
            ItemTemplate = new DataTemplate(() =>
            {
                var details = new Label { TextColor = UiKit.Ink };
                details.SetBinding(Label.TextProperty, nameof(RequestReview.Details));
                var assigned = new Label { FontSize = 12, TextColor = UiKit.Muted };
                assigned.SetBinding(Label.TextProperty, nameof(RequestReview.AssignedShift), stringFormat: "Roster: {0}");
                var outcome = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = UiKit.Accent };
                outcome.SetBinding(Label.TextProperty, nameof(RequestReview.Outcome));
                return UiKit.Card(new VerticalStackLayout { Spacing = 4, Children = { details, assigned, outcome } });
            })
        };
        requestsList.SetBinding(ItemsView.ItemsSourceProperty, nameof(RosterReviewViewModel.RequestComparisons));

        var status = new Label { FontSize = 13, TextColor = UiKit.Accent };
        status.SetBinding(Label.TextProperty, nameof(RosterReviewViewModel.StatusMessage));
        Content = new ScrollView
        {
            BackgroundColor = UiKit.Canvas,
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    UiKit.Heading("Roster review", 26),
                    UiKit.Caption("Review the generated/manual roster by staff member and compare every weighted request with the actual assignment."),
                    status,
                    UiKit.Heading("By staff", 18),
                    staffList,
                    UiKit.Heading("Requests vs assignments", 18),
                    requestsList
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
