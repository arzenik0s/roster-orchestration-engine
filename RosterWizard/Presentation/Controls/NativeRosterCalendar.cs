using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Controls;

public sealed class NativeRosterCalendar : ContentView
{
    private static readonly Color Accent = Color.FromArgb("#276C66");
    private static readonly Color AccentLight = Color.FromArgb("#E5F3F0");
    private static readonly Color BorderColor = Color.FromArgb("#DCE4E2");
    private static readonly Color MutedColor = Color.FromArgb("#71817E");

    public event Action<ShiftSlot>? SlotSelected;
    public event Action<DateOnly>? DateSelected;

    public void Render(RosterData data, DateOnly focusDate, CalendarViewKind viewKind)
    {
        var shiftsById = data.Shifts.ToDictionary(shift => shift.Id);
        var staffById = data.Staff.ToDictionary(staff => staff.Id);
        var slotsByDate = data.Slots
            .GroupBy(slot => slot.Date)
            .ToDictionary(group => group.Key, group => group.OrderBy(slot => shiftsById.GetValueOrDefault(slot.ShiftId)?.ShiftType).ToArray());

        Content = viewKind switch
        {
            CalendarViewKind.Month => BuildMonth(data, focusDate, slotsByDate, shiftsById, staffById),
            CalendarViewKind.Week => BuildPeriod(RosterCalendarViewModel.StartOfWeek(focusDate.ToDateTime(TimeOnly.MinValue)), 7, slotsByDate, shiftsById, staffById),
            CalendarViewKind.Day => BuildPeriod(focusDate.ToDateTime(TimeOnly.MinValue), 1, slotsByDate, shiftsById, staffById),
            _ => BuildList(data, focusDate, slotsByDate, shiftsById, staffById)
        };
    }

    private View BuildMonth(
        RosterData data,
        DateOnly focus,
        IReadOnlyDictionary<DateOnly, ShiftSlot[]> slotsByDate,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlyDictionary<Guid, StaffMember> staffById)
    {
        var grid = new Grid
        {
            ColumnSpacing = 6,
            RowSpacing = 6,
            Padding = new Thickness(2, 4)
        };
        for (var column = 0; column < 7; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        var weekdayLabels = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        for (var column = 0; column < weekdayLabels.Length; column++)
        {
            var header = new Label
            {
                Text = weekdayLabels[column],
                FontAttributes = FontAttributes.Bold,
                TextColor = MutedColor,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            grid.Add(header, column, 0);
        }

        var firstDay = focus.ToDateTime(TimeOnly.MinValue).AddDays(1 - focus.Day);
        var start = RosterCalendarViewModel.StartOfWeek(firstDay);
        var weeks = (int)Math.Ceiling(((firstDay - start).TotalDays + DateTime.DaysInMonth(focus.Year, focus.Month)) / 7);
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var row = 1; row <= weeks; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var index = 0; index < weeks * 7; index++)
        {
            var date = DateOnly.FromDateTime(start.AddDays(index));
            var dateButton = new Button
            {
                Text = date.Day.ToString(),
                FontAttributes = date == focus ? FontAttributes.Bold : FontAttributes.None,
                TextColor = date.Month == focus.Month ? Color.FromArgb("#243B38") : MutedColor,
                BackgroundColor = date == DateOnly.FromDateTime(DateTime.Today) ? AccentLight : Colors.Transparent,
                Padding = new Thickness(4, 2),
                HorizontalOptions = LayoutOptions.Start,
                HeightRequest = 30
            };
            dateButton.Clicked += (_, _) => DateSelected?.Invoke(date);

            var dayContent = new VerticalStackLayout { Spacing = 2 };
            dayContent.Add(dateButton);
            if (slotsByDate.TryGetValue(date, out var slots))
            {
                foreach (var slot in slots.Take(3))
                {
                    dayContent.Add(BuildSlotButton(slot, shiftsById, staffById, compact: true));
                }

                if (slots.Length > 3)
                {
                    dayContent.Add(new Label { Text = $"+{slots.Length - 3} more", FontSize = 10, TextColor = MutedColor, Margin = new Thickness(4, 0) });
                }
            }

            var dayCell = new Border
            {
                Stroke = BorderColor,
                StrokeThickness = 1,
                BackgroundColor = date.Month == focus.Month ? Colors.White : Color.FromArgb("#F6F8F7"),
                Padding = 4,
                Content = dayContent,
                MinimumHeightRequest = 105
            };
            grid.Add(dayCell, index % 7, index / 7 + 1);
        }

        return new ScrollView { Content = grid };
    }

    private View BuildPeriod(
        DateTime start,
        int numberOfDays,
        IReadOnlyDictionary<DateOnly, ShiftSlot[]> slotsByDate,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlyDictionary<Guid, StaffMember> staffById)
    {
        var grid = new Grid { ColumnSpacing = 8, Padding = new Thickness(2, 4) };
        for (var index = 0; index < numberOfDays; index++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(numberOfDays == 1 ? GridLength.Star : new GridLength(190)));
        }

        var dates = Enumerable.Range(0, numberOfDays).Select(offset => DateOnly.FromDateTime(start.AddDays(offset))).ToArray();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        for (var column = 0; column < dates.Length; column++)
        {
            var date = dates[column];
            var headerButton = new Button
            {
                Text = date.ToDateTime(TimeOnly.MinValue).ToString("ddd, dd MMM"),
                FontAttributes = FontAttributes.Bold,
                TextColor = Accent,
                BackgroundColor = Colors.Transparent
            };
            headerButton.Clicked += (_, _) => DateSelected?.Invoke(date);
            grid.Add(headerButton, column, 0);

            var dayEvents = new VerticalStackLayout { Spacing = 6 };
            if (slotsByDate.TryGetValue(date, out var slots))
            {
                foreach (var slot in slots)
                {
                    dayEvents.Add(BuildSlotButton(slot, shiftsById, staffById, compact: false));
                }
            }
            else
            {
                dayEvents.Add(new Label { Text = "No shifts", TextColor = MutedColor, Margin = new Thickness(6) });
            }

            grid.Add(new Border
            {
                Stroke = BorderColor,
                StrokeThickness = 1,
                Padding = 6,
                Content = dayEvents,
                VerticalOptions = LayoutOptions.Fill
            }, column, 1);
        }

        return new ScrollView
        {
            Orientation = numberOfDays == 1 ? ScrollOrientation.Vertical : ScrollOrientation.Both,
            Content = grid
        };
    }

    private View BuildList(
        RosterData data,
        DateOnly focus,
        IReadOnlyDictionary<DateOnly, ShiftSlot[]> slotsByDate,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlyDictionary<Guid, StaffMember> staffById)
    {
        var weekStart = DateOnly.FromDateTime(RosterCalendarViewModel.StartOfWeek(focus.ToDateTime(TimeOnly.MinValue)));
        var stack = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(2, 4) };
        for (var index = 0; index < 7; index++)
        {
            var date = weekStart.AddDays(index);
            if (!slotsByDate.TryGetValue(date, out var slots) || slots.Length == 0)
            {
                continue;
            }

            stack.Add(new Label
            {
                Text = date.ToDateTime(TimeOnly.MinValue).ToString("dddd, dd MMMM"),
                FontAttributes = FontAttributes.Bold,
                TextColor = Accent,
                Margin = new Thickness(4, 12, 4, 2)
            });
            foreach (var slot in slots)
            {
                stack.Add(BuildSlotButton(slot, shiftsById, staffById, compact: false));
            }
        }

        if (stack.Children.Count == 0)
        {
            stack.Add(new Label { Text = "No shifts in this week.", TextColor = MutedColor, Margin = new Thickness(8) });
        }

        return new ScrollView { Content = stack };
    }

    private Button BuildSlotButton(
        ShiftSlot slot,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlyDictionary<Guid, StaffMember> staffById,
        bool compact)
    {
        var shift = shiftsById.GetValueOrDefault(slot.ShiftId);
        var names = slot.StaffIds
            .Where(staffById.ContainsKey)
            .Select(id => staffById[id])
            .Select(staff => $"{staff.FirstName} {staff.LastName}".Trim());
        var text = compact
            ? shift?.ShiftType ?? "Shift"
            : $"{shift?.ShiftType ?? "Shift"}\n{string.Join(", ", names)}";
        var button = new Button
        {
            Text = text,
            FontSize = compact ? 10 : 13,
            Padding = new Thickness(6, compact ? 2 : 8),
            BackgroundColor = AccentLight,
            TextColor = Accent,
            HorizontalOptions = LayoutOptions.Fill,
            LineBreakMode = LineBreakMode.TailTruncation,
            CornerRadius = 6
        };
        button.Clicked += (_, _) => SlotSelected?.Invoke(slot);
        return button;
    }
}
