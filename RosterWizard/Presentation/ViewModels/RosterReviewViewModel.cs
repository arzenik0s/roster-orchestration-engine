using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Kronos.RosterWizard.Infrastructure;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public sealed record StaffRosterReview(Guid StaffId, string Name, string Email, int ShiftCount, string Assignments);
public sealed record RequestReview(string Details, string AssignedShift, string Outcome);

public partial class RosterReviewViewModel(IRosterRepository repository) : ViewModelBase
{
    public ObservableCollection<StaffRosterReview> StaffSchedules { get; } = [];
    public ObservableCollection<RequestReview> RequestComparisons { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await repository.GetSnapshotAsync();
        var staffById = data.Staff.ToDictionary(item => item.Id);
        var shiftsById = data.Shifts.ToDictionary(item => item.Id);
        var slots = data.Slots.OrderBy(item => item.Date).ToArray();

        StaffSchedules.Clear();
        foreach (var staff in data.Staff.OrderBy(item => item.LastName).ThenBy(item => item.FirstName))
        {
            var assignments = slots
                .Where(slot => slot.StaffIds.Contains(staff.Id))
                .Select(slot =>
                {
                    var shiftName = shiftsById.TryGetValue(slot.ShiftId, out var shift) ? shift.ShiftType : "Unknown shift";
                    return $"{slot.Date:ddd, dd MMM}  {shiftName}";
                })
                .ToArray();
            StaffSchedules.Add(new StaffRosterReview(
                staff.Id,
                $"{staff.FirstName} {staff.LastName}".Trim(),
                staff.Email,
                assignments.Length,
                assignments.Length == 0 ? "No assignments" : string.Join(Environment.NewLine, assignments)));
        }

        RequestComparisons.Clear();
        foreach (var request in data.Requests.OrderBy(item => item.Date).ThenBy(item => item.Priority))
        {
            var name = staffById.TryGetValue(request.StaffId, out var staff) ? $"{staff.FirstName} {staff.LastName}".Trim() : "Unknown staff";
            var shiftName = shiftsById.TryGetValue(request.ShiftId, out var shift) ? shift.ShiftType : "Unknown shift";
            var assigned = slots.Any(slot =>
                slot.Date == request.Date &&
                slot.ShiftId == request.ShiftId &&
                slot.StaffIds.Contains(request.StaffId));
            var honored = assigned == request.Like;
            RequestComparisons.Add(new RequestReview(
                $"{request.Date:ddd, dd MMM yyyy} · {name} · {(request.Like ? "likes" : "avoids")} {shiftName} · priority {request.Priority}",
                assigned ? "Assigned" : "Not assigned",
                honored ? "Honored" : "Not honored"));
        }

        StatusMessage = $"{data.Slots.Count} slots reviewed; {data.Requests.Count} staff requests compared against assignments.";
    }
}
