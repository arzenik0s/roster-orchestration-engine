using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Infrastructure;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public sealed record LeaveListItem(Guid Id, string DisplayText, StaffMember Staff);
public sealed record RequestListItem(Guid Id, string DisplayText, StaffMember Staff, ShiftDefinition Shift);

public partial class RequestsViewModel(IRosterRepository repository) : ViewModelBase
{
    public ObservableCollection<StaffMember> Staff { get; } = [];
    public ObservableCollection<ShiftDefinition> Shifts { get; } = [];
    public ObservableCollection<LeaveListItem> LeaveItems { get; } = [];
    public ObservableCollection<RequestListItem> RequestItems { get; } = [];

    [ObservableProperty] private StaffMember? selectedStaff;
    [ObservableProperty] private ShiftDefinition? selectedShift;
    [ObservableProperty] private DateTime requestDate = DateTime.Today;
    [ObservableProperty] private bool requestLike = true;
    [ObservableProperty] private int requestPriority = 1;
    [ObservableProperty] private string leaveDescription = "Leave";
    [ObservableProperty] private DateTime leaveDate = DateTime.Today;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await repository.GetSnapshotAsync();
        Staff.Clear();
        foreach (var member in data.Staff.OrderBy(item => item.LastName).ThenBy(item => item.FirstName))
        {
            Staff.Add(member);
        }

        Shifts.Clear();
        foreach (var shift in data.Shifts.OrderBy(item => item.ShiftType))
        {
            Shifts.Add(shift);
        }

        var staffById = data.Staff.ToDictionary(item => item.Id);
        var shiftsById = data.Shifts.ToDictionary(item => item.Id);
        LeaveItems.Clear();
        foreach (var leave in data.Leave.OrderBy(item => item.Date))
        {
            var member = staffById.GetValueOrDefault(leave.StaffId)
                ?? throw new InvalidDataException("Leave references an unknown staff member.");
            LeaveItems.Add(new LeaveListItem(leave.Id, $"{leave.Date:ddd, dd MMM yyyy} · {member.FirstName} {member.LastName} · {leave.Description}", member));
        }

        RequestItems.Clear();
        foreach (var request in data.Requests.OrderBy(item => item.Date))
        {
            var member = staffById.GetValueOrDefault(request.StaffId)
                ?? throw new InvalidDataException("A request references an unknown staff member.");
            var shift = shiftsById.GetValueOrDefault(request.ShiftId)
                ?? throw new InvalidDataException("A request references an unknown shift.");
            RequestItems.Add(new RequestListItem(request.Id,
                $"{request.Date:ddd, dd MMM yyyy} · {member.FirstName} {member.LastName} · {(request.Like ? "Likes" : "Avoids")} {shift.ShiftType} · priority {request.Priority}",
                member,
                shift));
        }

        SelectedStaff ??= Staff.FirstOrDefault();
        SelectedShift ??= Shifts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddLeaveAsync()
    {
        if (SelectedStaff is null || string.IsNullOrWhiteSpace(LeaveDescription))
        {
            StatusMessage = "Select a staff member and enter a leave description.";
            return;
        }

        await repository.UpdateAsync(data => data.Leave.Add(new LeaveEntry
        {
            StaffId = SelectedStaff.Id,
            Date = DateOnly.FromDateTime(LeaveDate),
            Description = LeaveDescription.Trim()
        }));
        StatusMessage = "Leave added.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddRequestAsync()
    {
        if (SelectedStaff is null || SelectedShift is null)
        {
            StatusMessage = "Select a staff member and shift for the request.";
            return;
        }

        await repository.UpdateAsync(data => data.Requests.Add(new StaffRequest
        {
            StaffId = SelectedStaff.Id,
            Date = DateOnly.FromDateTime(RequestDate),
            ShiftId = SelectedShift.Id,
            Like = RequestLike,
            Priority = Math.Max(1, RequestPriority)
        }));
        StatusMessage = "Staff request added.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteLeaveAsync(LeaveListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await repository.UpdateAsync(data => data.Leave.RemoveAll(leave => leave.Id == item.Id));
        StatusMessage = "Leave removed.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteRequestAsync(RequestListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await repository.UpdateAsync(data => data.Requests.RemoveAll(request => request.Id == item.Id));
        StatusMessage = "Request removed.";
        await LoadAsync();
    }
}
