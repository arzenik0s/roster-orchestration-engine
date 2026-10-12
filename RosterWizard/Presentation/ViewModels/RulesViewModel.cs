using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Infrastructure;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public partial class DaySelectionViewModel(RosterDay day) : ObservableObject
{
    public Guid Id { get; } = day.Id;
    public int Number { get; } = day.Number;

    [ObservableProperty]
    private bool isSelected;
}

public partial class SequenceStaffOptionViewModel(StaffMember staff) : ObservableObject
{
    public Guid Id { get; } = staff.Id;
    public string Name { get; } = $"{staff.FirstName} {staff.LastName}".Trim();

    [ObservableProperty]
    private bool isSelected;
}

public sealed record SkillMixRequirementItem(Guid Id, string DisplayText);
public sealed record ShiftSequencePositionItem(Guid Id, string DisplayText);

public partial class RulesViewModel(IRosterRepository repository) : ViewModelBase
{
    private RosterData? _currentData;

    public ObservableCollection<Role> Roles { get; } = [];
    public ObservableCollection<DayGroup> DayGroups { get; } = [];
    public ObservableCollection<RosterDay> Days { get; } = [];
    public ObservableCollection<ShiftDefinition> Shifts { get; } = [];
    public ObservableCollection<SkillMixRule> SkillMixRules { get; } = [];
    public ObservableCollection<SkillMixRequirementItem> Requirements { get; } = [];
    public ObservableCollection<ShiftSequence> Sequences { get; } = [];
    public ObservableCollection<ShiftSequencePositionItem> SequencePositions { get; } = [];
    public ObservableCollection<StaffMember> Staff { get; } = [];
    public ObservableCollection<DaySelectionViewModel> DayOptions { get; } = [];
    public ObservableCollection<SequenceStaffOptionViewModel> SequenceStaffOptions { get; } = [];

    [ObservableProperty] private string roleName = "";
    [ObservableProperty] private string dayGroupName = "";
    [ObservableProperty] private string shiftType = "";
    [ObservableProperty] private string skillMixRuleName = "";
    [ObservableProperty] private string sequenceName = "";
    [ObservableProperty] private int dayCount = 14;
    [ObservableProperty] private int requirementCount = 1;
    [ObservableProperty] private Role? selectedRole;
    [ObservableProperty] private DayGroup? selectedDayGroup;
    [ObservableProperty] private ShiftDefinition? selectedShift;
    [ObservableProperty] private SkillMixRule? selectedSkillMixRule;
    [ObservableProperty] private ShiftSequence? selectedSequence;
    [ObservableProperty] private ShiftDefinition? selectedPositionShift;
    [ObservableProperty] private StaffMember? selectedSequenceStaff;

    partial void OnSelectedSkillMixRuleChanged(SkillMixRule? value) => RefreshRequirements(value?.Id);
    partial void OnSelectedSequenceChanged(ShiftSequence? value) => RefreshSequencePositions(value?.Id);
    partial void OnSelectedDayGroupChanged(DayGroup? value) => RefreshDayOptions(value?.Id);

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await repository.GetSnapshotAsync();
        _currentData = data;
        Replace(Roles, data.Roles.OrderBy(item => item.RoleName));
        Replace(DayGroups, data.DayGroups.OrderBy(item => item.Name));
        Replace(Days, data.Days.OrderBy(item => item.Number));
        Replace(Shifts, data.Shifts.OrderBy(item => item.ShiftType));
        Replace(SkillMixRules, data.SkillMixRules.OrderBy(item => item.Name));
        Replace(Sequences, data.ShiftSequences.OrderBy(item => item.Name));
        Replace(Staff, data.Staff.OrderBy(item => item.LastName));
        Replace(SequenceStaffOptions, data.Staff.OrderBy(item => item.LastName)
            .Select(member => new SequenceStaffOptionViewModel(member)));
        if (SelectedDayGroup is null || DayGroups.All(item => item.Id != SelectedDayGroup.Id))
        {
            SelectedDayGroup = DayGroups.FirstOrDefault();
        }

        if (SelectedShift is null || Shifts.All(item => item.Id != SelectedShift.Id))
        {
            SelectedShift = Shifts.FirstOrDefault();
        }

        if (SelectedSkillMixRule is null || SkillMixRules.All(item => item.Id != SelectedSkillMixRule.Id))
        {
            SelectedSkillMixRule = SkillMixRules.FirstOrDefault();
        }

        if (SelectedSequence is null || Sequences.All(item => item.Id != SelectedSequence.Id))
        {
            SelectedSequence = Sequences.FirstOrDefault();
        }

        SelectedRole ??= Roles.FirstOrDefault();
        SelectedPositionShift ??= Shifts.FirstOrDefault();
        SelectedSequenceStaff ??= Staff.FirstOrDefault();
        DayCount = Math.Max(1, data.Days.Count);
        RefreshDayOptions(SelectedDayGroup?.Id);
        RefreshRequirements(SelectedSkillMixRule?.Id);
        RefreshSequencePositions(SelectedSequence?.Id);
    }

    [RelayCommand]
    private async Task AddRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(RoleName))
        {
            StatusMessage = "Enter a role name.";
            return;
        }

        await repository.UpdateAsync(data => data.Roles.Add(new Role { RoleName = RoleName.Trim() }));
        RoleName = "";
        StatusMessage = "Role created.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteRoleAsync(Role? role)
    {
        if (role is null)
        {
            return;
        }

        await repository.UpdateAsync(data =>
        {
            data.Roles.RemoveAll(item => item.Id == role.Id);
            foreach (var member in data.Staff)
            {
                member.RoleIds.RemoveAll(id => id == role.Id);
            }

            data.SkillMixRequirements.RemoveAll(item => item.RoleId == role.Id);
        });
        StatusMessage = "Role and linked staff/rule references removed.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SaveDaysAsync()
    {
        var count = Math.Clamp(DayCount, 1, 62);
        await repository.UpdateAsync(data =>
        {
            if (count < data.Days.Count)
            {
                var removedDayIds = data.Days.Where(day => day.Number > count).Select(day => day.Id).ToHashSet();
                data.Days.RemoveAll(day => removedDayIds.Contains(day.Id));
                data.DayGroupDays.RemoveAll(link => removedDayIds.Contains(link.DayId));
            }
            else if (count > data.Days.Count)
            {
                var currentMax = data.Days.Count == 0 ? 0 : data.Days.Max(day => day.Number);
                for (var number = currentMax + 1; number <= count; number++)
                {
                    data.Days.Add(new RosterDay { Number = number });
                }
            }
        });
        DayCount = count;
        StatusMessage = $"Roster template now contains {count} days.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddDayGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(DayGroupName))
        {
            StatusMessage = "Enter a day group name.";
            return;
        }

        await repository.UpdateAsync(data =>
        {
            var group = new DayGroup { Name = DayGroupName.Trim() };
            data.DayGroups.Add(group);
            data.DayGroupDays.AddRange(data.Days.Select(day => new DayGroupDay { DayGroupId = group.Id, DayId = day.Id }));
        });
        DayGroupName = "";
        StatusMessage = "Day group created and applied to all configured roster days.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SaveDayGroupDaysAsync()
    {
        if (SelectedDayGroup is null)
        {
            StatusMessage = "Select a day group.";
            return;
        }

        var groupId = SelectedDayGroup.Id;
        var selectedDayIds = DayOptions.Where(option => option.IsSelected).Select(option => option.Id).ToArray();
        await repository.UpdateAsync(data =>
        {
            data.DayGroupDays.RemoveAll(item => item.DayGroupId == groupId);
            data.DayGroupDays.AddRange(selectedDayIds.Select(dayId => new DayGroupDay
            {
                DayGroupId = groupId,
                DayId = dayId
            }));
        });
        StatusMessage = $"Saved {selectedDayIds.Length} roster days for {SelectedDayGroup.Name}.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteDayGroupAsync(DayGroup? group)
    {
        if (group is null)
        {
            return;
        }

        await repository.UpdateAsync(data =>
        {
            data.DayGroups.RemoveAll(item => item.Id == group.Id);
            data.DayGroupDays.RemoveAll(item => item.DayGroupId == group.Id);
            foreach (var shift in data.Shifts.Where(item => item.DayGroupId == group.Id))
            {
                shift.DayGroupId = null;
            }

            foreach (var sequence in data.ShiftSequences.Where(item => item.DayGroupId == group.Id))
            {
                sequence.DayGroupId = null;
            }
        });
        StatusMessage = "Day group removed; linked shifts and sequences now require a day-group selection.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(ShiftType) || SelectedDayGroup is null)
        {
            StatusMessage = "Enter a shift type and select its day group.";
            return;
        }

        await repository.UpdateAsync(data => data.Shifts.Add(new ShiftDefinition
        {
            ShiftType = ShiftType.Trim(),
            DayGroupId = SelectedDayGroup.Id
        }));
        ShiftType = "";
        StatusMessage = "Shift type created.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteShiftAsync(ShiftDefinition? shift)
    {
        if (shift is null)
        {
            return;
        }

        await repository.UpdateAsync(data =>
        {
            var ruleIds = data.SkillMixRules.Where(rule => rule.ShiftId == shift.Id).Select(rule => rule.Id).ToHashSet();
            data.Shifts.RemoveAll(item => item.Id == shift.Id);
            data.SkillMixRules.RemoveAll(item => item.ShiftId == shift.Id);
            data.SkillMixRequirements.RemoveAll(item => ruleIds.Contains(item.SkillMixRuleId));
            data.Slots.RemoveAll(item => item.ShiftId == shift.Id);
            data.Requests.RemoveAll(item => item.ShiftId == shift.Id);
            data.ShiftSequenceShifts.RemoveAll(item => item.ShiftId == shift.Id);
        });
        StatusMessage = "Shift type and linked slots, requests, rules, and sequence positions removed.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddSkillMixRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(SkillMixRuleName) || SelectedShift is null)
        {
            StatusMessage = "Enter a skill-mix rule name and select a shift.";
            return;
        }

        await repository.UpdateAsync(data => data.SkillMixRules.Add(new SkillMixRule
        {
            Name = SkillMixRuleName.Trim(),
            ShiftId = SelectedShift.Id
        }));
        SkillMixRuleName = "";
        StatusMessage = "Skill-mix rule created. Add role counts to complete it.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteSkillMixRuleAsync(SkillMixRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        await repository.UpdateAsync(data =>
        {
            data.SkillMixRules.RemoveAll(item => item.Id == rule.Id);
            data.SkillMixRequirements.RemoveAll(item => item.SkillMixRuleId == rule.Id);
        });
        StatusMessage = "Skill-mix rule and role counts removed.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteRequirementAsync(SkillMixRequirementItem? requirement)
    {
        if (requirement is null)
        {
            return;
        }

        await repository.UpdateAsync(data => data.SkillMixRequirements.RemoveAll(item => item.Id == requirement.Id));
        StatusMessage = "Skill-mix role count removed.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddRequirementAsync()
    {
        if (SelectedSkillMixRule is null || SelectedRole is null)
        {
            StatusMessage = "Select a skill-mix rule and role.";
            return;
        }

        await repository.UpdateAsync(data =>
        {
            var previous = data.SkillMixRequirements.SingleOrDefault(item =>
                item.SkillMixRuleId == SelectedSkillMixRule.Id && item.RoleId == SelectedRole.Id);
            if (previous is null)
            {
                data.SkillMixRequirements.Add(new SkillMixRequirement
                {
                    SkillMixRuleId = SelectedSkillMixRule.Id,
                    RoleId = SelectedRole.Id,
                    Count = Math.Max(0, RequirementCount)
                });
            }
            else
            {
                previous.Count = Math.Max(0, RequirementCount);
            }
        });
        StatusMessage = "Skill-mix role count saved.";
        await LoadAsync();
        SelectedSkillMixRule = SkillMixRules.FirstOrDefault(item => item.Id == SelectedSkillMixRule?.Id);
    }

    [RelayCommand]
    private async Task AddSequenceAsync()
    {
        var staffIds = SequenceStaffOptions.Where(option => option.IsSelected).Select(option => option.Id).ToList();
        if (string.IsNullOrWhiteSpace(SequenceName) || SelectedDayGroup is null || staffIds.Count == 0)
        {
            StatusMessage = "Enter a sequence name, select its day group, and select at least one staff member.";
            return;
        }

        await repository.UpdateAsync(data => data.ShiftSequences.Add(new ShiftSequence
        {
            Name = SequenceName.Trim(),
            DayGroupId = SelectedDayGroup.Id,
            StaffIds = staffIds
        }));
        SequenceName = "";
        foreach (var option in SequenceStaffOptions)
        {
            option.IsSelected = false;
        }
        StatusMessage = "Shift sequence created.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddSequencePositionAsync()
    {
        if (SelectedSequence is null)
        {
            StatusMessage = "Select a shift sequence first.";
            return;
        }

        await repository.UpdateAsync(data =>
        {
            var position = data.ShiftSequenceShifts
                .Where(item => item.ShiftSequenceId == SelectedSequence.Id)
                .Select(item => item.Position)
                .DefaultIfEmpty(0)
                .Max() + 1;
            data.ShiftSequenceShifts.Add(new ShiftSequenceShift
            {
                ShiftSequenceId = SelectedSequence.Id,
                Position = position,
                ShiftId = SelectedPositionShift?.Id
            });
        });
        StatusMessage = "Shift sequence position appended.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteSequencePositionAsync(ShiftSequencePositionItem? position)
    {
        if (position is null)
        {
            return;
        }

        await repository.UpdateAsync(data => data.ShiftSequenceShifts.RemoveAll(item => item.Id == position.Id));
        StatusMessage = "Sequence position removed.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteSequenceAsync(ShiftSequence? sequence)
    {
        if (sequence is null)
        {
            return;
        }

        await repository.UpdateAsync(data =>
        {
            data.ShiftSequences.RemoveAll(item => item.Id == sequence.Id);
            data.ShiftSequenceShifts.RemoveAll(item => item.ShiftSequenceId == sequence.Id);
        });
        StatusMessage = "Shift sequence and its ordered positions removed.";
        await LoadAsync();
    }

    private void RefreshRequirements(Guid? ruleId)
    {
        if (ruleId is null || _currentData is null)
        {
            Requirements.Clear();
            return;
        }

        var roleNames = _currentData.Roles.ToDictionary(item => item.Id, item => item.RoleName);
        var requirements = _currentData.SkillMixRequirements
            .Where(item => item.SkillMixRuleId == ruleId.Value)
            .Select(item => new SkillMixRequirementItem(item.Id,
                $"{roleNames.GetValueOrDefault(item.RoleId, "Unknown role")}: {item.Count}"));
        Replace(Requirements, requirements);
    }

    private void RefreshSequencePositions(Guid? sequenceId)
    {
        if (sequenceId is null || _currentData is null)
        {
            SequencePositions.Clear();
            return;
        }

        var shiftNames = _currentData.Shifts.ToDictionary(item => item.Id, item => item.ShiftType);
        var positions = _currentData.ShiftSequenceShifts
            .Where(item => item.ShiftSequenceId == sequenceId.Value)
            .OrderBy(item => item.Position)
            .Select(item => new ShiftSequencePositionItem(item.Id,
                $"{item.Position}. {(item.ShiftId.HasValue ? shiftNames.GetValueOrDefault(item.ShiftId.Value, "Unknown shift") : "Off")}"));
        Replace(SequencePositions, positions);
    }

    private void RefreshDayOptions(Guid? groupId)
    {
        if (_currentData is null)
        {
            return;
        }

        var assigned = groupId.HasValue
            ? _currentData.DayGroupDays.Where(item => item.DayGroupId == groupId.Value).Select(item => item.DayId).ToHashSet()
            : [];
        Replace(DayOptions, _currentData.Days.OrderBy(item => item.Number)
            .Select(day => new DaySelectionViewModel(day) { IsSelected = assigned.Contains(day.Id) }));
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var item in values)
        {
            collection.Add(item);
        }
    }
}
