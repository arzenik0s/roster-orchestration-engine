namespace Kronos.RosterWizard.Domain;

public sealed class RosterData
{
    public int SchemaVersion { get; set; } = 1;
    public RosterSettings Settings { get; set; } = new();
    public List<StaffMember> Staff { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
    public List<DayGroup> DayGroups { get; set; } = [];
    public List<RosterDay> Days { get; set; } = [];
    public List<DayGroupDay> DayGroupDays { get; set; } = [];
    public List<ShiftDefinition> Shifts { get; set; } = [];
    public List<ShiftSlot> Slots { get; set; } = [];
    public List<LeaveEntry> Leave { get; set; } = [];
    public List<StaffRequest> Requests { get; set; } = [];
    public List<ShiftSequence> ShiftSequences { get; set; } = [];
    public List<ShiftSequenceShift> ShiftSequenceShifts { get; set; } = [];
    public List<SkillMixRule> SkillMixRules { get; set; } = [];
    public List<SkillMixRequirement> SkillMixRequirements { get; set; } = [];
}

public sealed class RosterSettings
{
    public string RosterName { get; set; } = "Roster";
}

public sealed class StaffMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
    public List<Guid> RoleIds { get; set; } = [];
    public int ShiftsPerRoster { get; set; }
    public bool EnforceShiftsPerRoster { get; set; } = true;
    public bool EnforceOneShiftPerDay { get; set; } = true;
    public bool MaxShifts { get; set; } = true;
}

public sealed class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = "";
}

public sealed class DayGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}

public sealed class RosterDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Number { get; set; }
}

public sealed class DayGroupDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DayGroupId { get; set; }
    public Guid DayId { get; set; }
}

public sealed class ShiftDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShiftType { get; set; } = "";
    public Guid? DayGroupId { get; set; }
}

public sealed class ShiftSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public Guid ShiftId { get; set; }
    public List<Guid> StaffIds { get; set; } = [];
}

public sealed class LeaveEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = "Leave";
}

public sealed class StaffRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ShiftId { get; set; }
    public bool Like { get; set; } = true;
    public int Priority { get; set; } = 1;
}

public sealed class ShiftSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<Guid> StaffIds { get; set; } = [];
    public Guid? DayGroupId { get; set; }
}

public sealed class ShiftSequenceShift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShiftSequenceId { get; set; }
    public int Position { get; set; }
    public Guid? ShiftId { get; set; }
}

public sealed class SkillMixRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Guid ShiftId { get; set; }
}

public sealed class SkillMixRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkillMixRuleId { get; set; }
    public Guid RoleId { get; set; }
    public int Count { get; set; }
}
