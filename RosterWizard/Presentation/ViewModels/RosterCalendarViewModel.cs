using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Infrastructure;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public enum CalendarViewKind
{
    Month,
    Week,
    Day,
    List
}

public partial class RosterCalendarViewModel(IRosterRepository repository) : ViewModelBase
{
    [ObservableProperty] private DateTime currentDate = DateTime.Today;
    [ObservableProperty] private CalendarViewKind viewKind = CalendarViewKind.Month;
    [ObservableProperty] private string title = "";
    [ObservableProperty] private RosterData? roster;

    partial void OnCurrentDateChanged(DateTime value) => UpdateTitle();
    partial void OnViewKindChanged(CalendarViewKind value) => UpdateTitle();

    [RelayCommand]
    public async Task LoadAsync()
    {
        Roster = await repository.GetSnapshotAsync();
        UpdateTitle();
    }

    [RelayCommand]
    private void Previous()
    {
        CurrentDate = ViewKind switch
        {
            CalendarViewKind.Month => CurrentDate.AddMonths(-1),
            CalendarViewKind.Week or CalendarViewKind.List => CurrentDate.AddDays(-7),
            _ => CurrentDate.AddDays(-1)
        };
    }

    [RelayCommand]
    private void Next()
    {
        CurrentDate = ViewKind switch
        {
            CalendarViewKind.Month => CurrentDate.AddMonths(1),
            CalendarViewKind.Week or CalendarViewKind.List => CurrentDate.AddDays(7),
            _ => CurrentDate.AddDays(1)
        };
    }

    [RelayCommand]
    private void Today() => CurrentDate = DateTime.Today;

    private void UpdateTitle()
    {
        var date = CurrentDate.Date;
        Title = ViewKind switch
        {
            CalendarViewKind.Month => date.ToString("MMMM yyyy"),
            CalendarViewKind.Week or CalendarViewKind.List => WeekTitle(date),
            _ => date.ToString("dddd, dd MMMM yyyy")
        };
    }

    private static string WeekTitle(DateTime date)
    {
        var start = StartOfWeek(date);
        return $"{start:dd MMM} – {start.AddDays(6):dd MMM yyyy}";
    }

    internal static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceSunday = (int)date.DayOfWeek;
        return date.Date.AddDays(-daysSinceSunday);
    }
}
