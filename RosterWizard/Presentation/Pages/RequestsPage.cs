using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Pages;

public sealed class RequestsPage : ContentPage
{
    private readonly RequestsViewModel _viewModel;

    public RequestsPage(RequestsViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Requests & leave";
        BackgroundColor = UiKit.Canvas;

        var leaveCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Leave", 18),
                StaffPicker(nameof(RequestsViewModel.SelectedStaff)),
                DatePickerFor(nameof(RequestsViewModel.LeaveDate)),
                EntryFor(nameof(RequestsViewModel.LeaveDescription), "Description"),
                UiKit.Action("Add leave", viewModel.AddLeaveCommand),
                MakeItems(viewModel.LeaveItems, nameof(RequestsViewModel.DeleteLeaveCommand))
            }
        };
        var requestCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Staff shift requests", 18),
                StaffPicker(nameof(RequestsViewModel.SelectedStaff)),
                new Picker { Title = "Shift" },
                DatePickerFor(nameof(RequestsViewModel.RequestDate)),
                CheckOption("Like this shift", nameof(RequestsViewModel.RequestLike)),
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        UiKit.Caption("Priority"),
                        new Entry { Keyboard = Keyboard.Numeric, WidthRequest = 90 }
                    }
                },
                UiKit.Action("Add request", viewModel.AddRequestCommand),
                MakeItems(viewModel.RequestItems, nameof(RequestsViewModel.DeleteRequestCommand))
            }
        };
        var requestShiftPicker = (Picker)requestCard.Children[2];
        requestShiftPicker.SetBinding(Picker.ItemsSourceProperty, nameof(RequestsViewModel.Shifts));
        requestShiftPicker.ItemDisplayBinding = new Binding(nameof(ShiftDefinition.ShiftType));
        requestShiftPicker.SetBinding(Picker.SelectedItemProperty, nameof(RequestsViewModel.SelectedShift));
        ((Entry)((HorizontalStackLayout)requestCard.Children[5]).Children[1])
            .SetBinding(Entry.TextProperty, nameof(RequestsViewModel.RequestPriority));
        ((Entry)((HorizontalStackLayout)requestCard.Children[5]).Children[1]).TextChanged += (_, args) =>
        {
            if (int.TryParse(args.NewTextValue, out var value))
            {
                viewModel.RequestPriority = value;
            }
        };

        var status = new Label { TextColor = UiKit.Accent, FontSize = 13 };
        status.SetBinding(Label.TextProperty, nameof(RequestsViewModel.StatusMessage));
        var contentGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 14
        };
        contentGrid.Add(UiKit.Card(leaveCard), 0, 0);
        contentGrid.Add(UiKit.Card(requestCard), 1, 0);
        Content = new ScrollView
        {
            BackgroundColor = UiKit.Canvas,
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    UiKit.Heading("Requests & leave", 26),
                    UiKit.Caption("Requests are weighted preferences; leave is a hard constraint during roster generation."),
                    status,
                    contentGrid
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private Picker StaffPicker(string bindingProperty)
    {
        var picker = new Picker { Title = "Staff member" };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(RequestsViewModel.Staff));
        picker.ItemDisplayBinding = new Binding(nameof(StaffMember.Email));
        picker.SetBinding(Picker.SelectedItemProperty, bindingProperty);
        return picker;
    }

    private DatePicker DatePickerFor(string bindingProperty)
    {
        var picker = new DatePicker { Date = DateTime.Today };
        picker.SetBinding(DatePicker.DateProperty, bindingProperty);
        return picker;
    }

    private Entry EntryFor(string bindingProperty, string placeholder)
    {
        var entry = new Entry { Placeholder = placeholder };
        entry.SetBinding(Entry.TextProperty, bindingProperty);
        return entry;
    }

    private View CheckOption(string text, string property)
    {
        var checkBox = new CheckBox();
        checkBox.SetBinding(CheckBox.IsCheckedProperty, property);
        return new HorizontalStackLayout
        {
            Spacing = 6,
            Children = { checkBox, new Label { Text = text, VerticalTextAlignment = TextAlignment.Center } }
        };
    }

    private CollectionView MakeItems<T>(IEnumerable<T> items, string commandName)
    {
        var collection = new CollectionView
        {
            HeightRequest = 230,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { VerticalTextAlignment = TextAlignment.Center, FontSize = 13 };
                var remove = UiKit.Secondary("Remove");
                var row = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    ColumnSpacing = 8,
                    Padding = new Thickness(2, 4)
                };
                row.Add(label, 0, 0);
                row.Add(remove, 1, 0);
                var property = commandName == nameof(RequestsViewModel.DeleteLeaveCommand)
                    ? nameof(LeaveListItem.DisplayText)
                    : nameof(RequestListItem.DisplayText);
                label.SetBinding(Label.TextProperty, property);
                remove.Clicked += async (_, _) =>
                {
                    if (commandName == nameof(RequestsViewModel.DeleteLeaveCommand))
                    {
                        await _viewModel.DeleteLeaveCommand.ExecuteAsync(remove.BindingContext as LeaveListItem);
                    }
                    else
                    {
                        await _viewModel.DeleteRequestCommand.ExecuteAsync(remove.BindingContext as RequestListItem);
                    }
                };
                remove.SetBinding(Button.CommandParameterProperty, new Binding("."));
                return row;
            })
        };
        collection.ItemsSource = items;
        return collection;
    }
}
