using System.ComponentModel;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Infrastructure;
using Kronos.RosterWizard.Presentation.Controls;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Pages;

public sealed class RosterCalendarPage : ContentPage
{
    private readonly RosterCalendarViewModel _viewModel;
    private readonly IRosterRepository _repository;
    private readonly Label _title = new();
    private readonly NativeRosterCalendar _calendar = new();
    private readonly Label _status = new();

    public RosterCalendarPage(RosterCalendarViewModel viewModel, IRosterRepository repository)
    {
        _viewModel = viewModel;
        _repository = repository;
        BindingContext = viewModel;
        Title = "Roster calendar";
        BackgroundColor = UiKit.Canvas;
        _title.SetBinding(Label.TextProperty, nameof(RosterCalendarViewModel.Title));
        _status.SetBinding(Label.TextProperty, nameof(RosterCalendarViewModel.StatusMessage));
        _status.TextColor = UiKit.Accent;
        _status.FontSize = 13;

        var viewPicker = new Picker
        {
            Title = "Calendar view",
            ItemsSource = Enum.GetValues<CalendarViewKind>(),
            WidthRequest = 150
        };
        viewPicker.SetBinding(Picker.SelectedItemProperty, nameof(RosterCalendarViewModel.ViewKind));
        var toolbar = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                UiKit.Secondary("‹", viewModel.PreviousCommand),
                UiKit.Secondary("Today", viewModel.TodayCommand),
                UiKit.Secondary("›", viewModel.NextCommand),
                viewPicker
            }
        };

        _calendar.SlotSelected += async slot => await EditSlotAsync(slot);
        _calendar.DateSelected += date =>
        {
            viewModel.CurrentDate = date.ToDateTime(TimeOnly.MinValue);
            viewModel.ViewKind = CalendarViewKind.Day;
        };
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        var pageGrid = new Grid
        {
            BackgroundColor = UiKit.Canvas,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            Padding = new Thickness(20),
            RowSpacing = 10
        };
        pageGrid.Add(new VerticalStackLayout { Spacing = 8, Children = { UiKit.Heading("Roster calendar", 26), toolbar, _title } }, 0, 0);
        pageGrid.Add(UiKit.Card(_calendar), 0, 1);
        pageGrid.Add(_status, 0, 2);
        Content = pageGrid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
        RenderCalendar();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RosterCalendarViewModel.CurrentDate)
            or nameof(RosterCalendarViewModel.ViewKind)
            or nameof(RosterCalendarViewModel.Roster))
        {
            RenderCalendar();
        }
    }

    private void RenderCalendar()
    {
        if (_viewModel.Roster is not null)
        {
            _calendar.Render(_viewModel.Roster, DateOnly.FromDateTime(_viewModel.CurrentDate), _viewModel.ViewKind);
        }
    }

    private async Task EditSlotAsync(ShiftSlot selectedSlot)
    {
        var data = await _repository.GetSnapshotAsync();
        var slot = data.Slots.SingleOrDefault(item => item.Id == selectedSlot.Id);
        if (slot is null)
        {
            _status.Text = "This shift slot is no longer available. Refresh and try again.";
            return;
        }

        var shiftName = data.Shifts.Single(item => item.Id == slot.ShiftId).ShiftType;
        var staffById = data.Staff.ToDictionary(item => item.Id);
        var actions = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var staff in data.Staff.OrderBy(item => item.LastName).ThenBy(item => item.FirstName))
        {
            var label = slot.StaffIds.Contains(staff.Id) ? $"Remove: {staff.FirstName} {staff.LastName}" : $"Assign: {staff.FirstName} {staff.LastName}";
            actions.Add(label, staff.Id);
        }

        var selected = await DisplayActionSheetAsync(
            $"{shiftName} · {slot.Date:ddd, dd MMM yyyy}",
            "Cancel",
            "Clear all assignments",
            actions.Keys.ToArray());
        if (selected == "Clear all assignments")
        {
            await _repository.UpdateAsync(data =>
            {
                var current = data.Slots.Single(item => item.Id == slot.Id);
                current.StaffIds.Clear();
            });
        }
        else if (actions.TryGetValue(selected ?? "", out var staffId))
        {
            await _repository.UpdateAsync(data =>
            {
                var current = data.Slots.Single(item => item.Id == slot.Id);
                if (current.StaffIds.Contains(staffId))
                {
                    current.StaffIds.Remove(staffId);
                }
                else
                {
                    current.StaffIds.Add(staffId);
                }
            });
        }
        else
        {
            return;
        }

        _status.Text = $"Assignments updated for {shiftName}. Manual edits are saved as entered; run generation to re-apply all roster constraints.";
        await _viewModel.LoadAsync();
        RenderCalendar();
    }
}
