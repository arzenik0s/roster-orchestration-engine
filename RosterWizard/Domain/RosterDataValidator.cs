namespace Kronos.RosterWizard.Domain;

public static class RosterDataValidator
{
    public static void Validate(RosterData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Roster data version {data.SchemaVersion} is not supported.");
        }

        EnsureUnique(data.Staff.Select(item => item.Id), "staff member");
        EnsureUnique(data.Roles.Select(item => item.Id), "role");
        EnsureUnique(data.DayGroups.Select(item => item.Id), "day group");
        EnsureUnique(data.Days.Select(item => item.Id), "roster day");
        EnsureUnique(data.Shifts.Select(item => item.Id), "shift");
        EnsureUnique(data.Slots.Select(item => item.Id), "time slot");
        EnsureUnique(data.Leave.Select(item => item.Id), "leave entry");
        EnsureUnique(data.Requests.Select(item => item.Id), "staff request");
        EnsureUnique(data.ShiftSequences.Select(item => item.Id), "shift sequence");
        EnsureUnique(data.ShiftSequenceShifts.Select(item => item.Id), "shift sequence item");
        EnsureUnique(data.SkillMixRules.Select(item => item.Id), "skill mix rule");
        EnsureUnique(data.SkillMixRequirements.Select(item => item.Id), "skill mix requirement");

        var staffIds = data.Staff.Select(item => item.Id).ToHashSet();
        var roleIds = data.Roles.Select(item => item.Id).ToHashSet();
        var groupIds = data.DayGroups.Select(item => item.Id).ToHashSet();
        var dayIds = data.Days.Select(item => item.Id).ToHashSet();
        var shiftIds = data.Shifts.Select(item => item.Id).ToHashSet();
        var ruleIds = data.SkillMixRules.Select(item => item.Id).ToHashSet();
        var sequenceIds = data.ShiftSequences.Select(item => item.Id).ToHashSet();

        if (data.Days.Any(day => day.Number < 1)
            || !data.Days.Select(day => day.Number).Order().SequenceEqual(Enumerable.Range(1, data.Days.Count)))
        {
            throw new InvalidDataException("Roster day numbers must be consecutive, starting at 1.");
        }

        EnsureUniqueKeys(data.Staff.SelectMany(member => member.RoleIds.Select(roleId => (member.Id, roleId))), "staff role membership");
        EnsureUniqueKeys(data.DayGroupDays.Select(link => (link.DayGroupId, link.DayId)), "day group day");
        EnsureUniqueKeys(data.Slots.Select(slot => (slot.Date, slot.ShiftId)), "date and shift slot");
        EnsureUniqueKeys(data.SkillMixRequirements.Select(item => (item.SkillMixRuleId, item.RoleId)), "skill mix role requirement");
        EnsureUniqueKeys(data.Leave.Select(item => (item.StaffId, item.Date)), "staff leave date");

        RequireReferences(data.Staff.SelectMany(item => item.RoleIds), roleIds, "staff role");
        RequireReferences(data.DayGroupDays.Select(item => item.DayGroupId), groupIds, "day group");
        RequireReferences(data.DayGroupDays.Select(item => item.DayId), dayIds, "roster day");
        RequireReferences(data.Shifts.Where(item => item.DayGroupId.HasValue).Select(item => item.DayGroupId!.Value), groupIds, "shift day group");
        RequireReferences(data.Slots.Select(item => item.ShiftId), shiftIds, "time slot shift");
        RequireReferences(data.Slots.SelectMany(item => item.StaffIds), staffIds, "time slot staff member");
        RequireReferences(data.Leave.Select(item => item.StaffId), staffIds, "leave staff member");
        RequireReferences(data.Requests.Select(item => item.StaffId), staffIds, "request staff member");
        RequireReferences(data.Requests.Select(item => item.ShiftId), shiftIds, "request shift");
        RequireReferences(data.ShiftSequences.SelectMany(item => item.StaffIds), staffIds, "sequence staff member");
        RequireReferences(data.ShiftSequences.Where(item => item.DayGroupId.HasValue).Select(item => item.DayGroupId!.Value), groupIds, "sequence day group");
        RequireReferences(data.ShiftSequenceShifts.Select(item => item.ShiftSequenceId), sequenceIds, "sequence item parent");
        RequireReferences(data.ShiftSequenceShifts.Where(item => item.ShiftId.HasValue).Select(item => item.ShiftId!.Value), shiftIds, "sequence item shift");
        RequireReferences(data.SkillMixRules.Select(item => item.ShiftId), shiftIds, "skill mix shift");
        RequireReferences(data.SkillMixRequirements.Select(item => item.SkillMixRuleId), ruleIds, "skill mix requirement rule");
        RequireReferences(data.SkillMixRequirements.Select(item => item.RoleId), roleIds, "skill mix requirement role");

        if (data.SkillMixRequirements.Any(requirement => requirement.Count < 0))
        {
            throw new InvalidDataException("Skill mix counts cannot be negative.");
        }

        if (data.Slots.Any(slot => slot.StaffIds.Distinct().Count() != slot.StaffIds.Count)
            || data.ShiftSequences.Any(sequence => sequence.StaffIds.Distinct().Count() != sequence.StaffIds.Count))
        {
            throw new InvalidDataException("Roster slots and shift sequences cannot contain duplicate staff references.");
        }
    }

    private static void EnsureUnique(IEnumerable<Guid> ids, string entityName)
    {
        var values = ids.ToArray();
        if (values.Any(id => id == Guid.Empty) || values.Distinct().Count() != values.Length)
        {
            throw new InvalidDataException($"Each {entityName} must have a unique, non-empty identifier.");
        }
    }

    private static void RequireReferences(IEnumerable<Guid> references, HashSet<Guid> knownIds, string relationName)
    {
        if (references.Any(reference => !knownIds.Contains(reference)))
        {
            throw new InvalidDataException($"Roster data contains an invalid {relationName} reference.");
        }
    }

    private static void EnsureUniqueKeys<TKey>(IEnumerable<TKey> keys, string entityName)
        where TKey : notnull
    {
        var values = keys.ToArray();
        if (values.Distinct().Count() != values.Length)
        {
            throw new InvalidDataException($"Each {entityName} must be unique.");
        }
    }
}
