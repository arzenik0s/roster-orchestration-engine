using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Infrastructure;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public partial class RoleSelectionViewModel(Role role) : ObservableObject
{
    public Guid Id { get; } = role.Id;
    public string Name { get; } = role.RoleName;

    [ObservableProperty]
    private bool isSelected;
}

public partial class StaffViewModel(IRosterRepository repository) : ViewModelBase
{
    public ObservableCollection<StaffMember> Staff { get; } = [];
    public ObservableCollection<RoleSelectionViewModel> RoleOptions { get; } = [];

    [ObservableProperty] private StaffMember? selectedStaff;
    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private int shiftsPerRoster;
    [ObservableProperty] private bool isAvailable = true;
    [ObservableProperty] private bool enforceShiftsPerRoster = true;
    [ObservableProperty] private bool enforceOneShiftPerDay = true;
    [ObservableProperty] private bool maxShifts = true;

    partial void OnSelectedStaffChanged(StaffMember? value)
    {
        if (value is null)
        {
            return;
        }

        FirstName = value.FirstName;
        LastName = value.LastName;
        Email = value.Email;
        ShiftsPerRoster = value.ShiftsPerRoster;
        IsAvailable = value.IsAvailable;
        EnforceShiftsPerRoster = value.EnforceShiftsPerRoster;
        EnforceOneShiftPerDay = value.EnforceOneShiftPerDay;
        MaxShifts = value.MaxShifts;
        foreach (var option in RoleOptions)
        {
            option.IsSelected = value.RoleIds.Contains(option.Id);
        }
    }

    [RelayCommand]
    private void NewStaff()
    {
        SelectedStaff = null;
        FirstName = LastName = Email = "";
        ShiftsPerRoster = 0;
        IsAvailable = EnforceShiftsPerRoster = EnforceOneShiftPerDay = true;
        MaxShifts = true;
        foreach (var option in RoleOptions)
        {
            option.IsSelected = false;
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await repository.GetSnapshotAsync();
        Staff.Clear();
        foreach (var member in data.Staff.OrderBy(item => item.LastName).ThenBy(item => item.FirstName))
        {
            Staff.Add(member);
        }

        var previousSelections = RoleOptions.Where(option => option.IsSelected).Select(option => option.Id).ToHashSet();
        RoleOptions.Clear();
        foreach (var role in data.Roles.OrderBy(item => item.RoleName))
        {
            RoleOptions.Add(new RoleSelectionViewModel(role) { IsSelected = previousSelections.Contains(role.Id) });
        }
    }

    [RelayCommand]
    private async Task SaveStaffAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = "Enter a first name, last name, and email address.";
            return;
        }

        await repository.UpdateAsync(data =>
        {
            var member = SelectedStaff is null
                ? new StaffMember()
                : data.Staff.SingleOrDefault(item => item.Id == SelectedStaff.Id)
                    ?? throw new InvalidOperationException("The selected staff member no longer exists.");
            member.FirstName = FirstName.Trim();
            member.LastName = LastName.Trim();
            member.Email = Email.Trim();
            member.ShiftsPerRoster = Math.Max(0, ShiftsPerRoster);
            member.IsAvailable = IsAvailable;
            member.EnforceShiftsPerRoster = EnforceShiftsPerRoster;
            member.EnforceOneShiftPerDay = EnforceOneShiftPerDay;
            member.MaxShifts = MaxShifts;
            member.RoleIds = RoleOptions.Where(option => option.IsSelected).Select(option => option.Id).ToList();
            if (SelectedStaff is null)
            {
                data.Staff.Add(member);
            }
        });

        StatusMessage = "Staff member saved.";
        SelectedStaff = null;
        FirstName = LastName = Email = "";
        ShiftsPerRoster = 0;
        IsAvailable = EnforceShiftsPerRoster = EnforceOneShiftPerDay = true;
        MaxShifts = true;
        foreach (var option in RoleOptions)
        {
            option.IsSelected = false;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedStaff is null)
        {
            StatusMessage = "Select a staff member to delete.";
            return;
        }

        var id = SelectedStaff.Id;
        await repository.UpdateAsync(data =>
        {
            data.Staff.RemoveAll(item => item.Id == id);
            data.Leave.RemoveAll(item => item.StaffId == id);
            data.Requests.RemoveAll(item => item.StaffId == id);
            foreach (var slot in data.Slots)
            {
                slot.StaffIds.RemoveAll(staffId => staffId == id);
            }

            foreach (var sequence in data.ShiftSequences)
            {
                sequence.StaffIds.RemoveAll(staffId => staffId == id);
            }
        });

        SelectedStaff = null;
        StatusMessage = "Staff member and linked roster assignments, leave, requests, and sequences were removed.";
        await LoadAsync();
    }
}
