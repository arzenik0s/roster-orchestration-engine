using Google.OrTools.Sat;
using Kronos.RosterWizard.Domain;

namespace Kronos.RosterWizard.Application.Services;

public sealed record GenerationResult(DateOnly StartDate, DateOnly EndDate, string SolverStatus, long RequestScore);

public interface IRosterGenerationService
{
    GenerationResult Generate(RosterData data, DateOnly startDate);
}

public sealed class RosterGenerationService : IRosterGenerationService
{
    private sealed record AssignmentKey(Guid StaffId, DateOnly Date, Guid ShiftId);
    private sealed record PlannedSlot(DateOnly Date, ShiftDefinition Shift);

    public GenerationResult Generate(RosterData data, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Days.Count == 0)
        {
            throw new InvalidOperationException("Configure at least one roster day before generating a roster.");
        }

        var periodLength = data.Days.Count;
        var endDate = startDate.AddDays(periodLength - 1);
        var dayNumberById = data.Days.ToDictionary(day => day.Id, day => day.Number);
        var groupDays = data.DayGroupDays
            .Where(link => dayNumberById.ContainsKey(link.DayId))
            .GroupBy(link => link.DayGroupId)
            .ToDictionary(group => group.Key, group => group.Select(link => dayNumberById[link.DayId]).ToHashSet());
        var plannedSlots = (from day in data.Days
                            join link in data.DayGroupDays on day.Id equals link.DayId
                            join shift in data.Shifts on link.DayGroupId equals shift.DayGroupId
                            where shift.DayGroupId.HasValue
                            select new PlannedSlot(startDate.AddDays(day.Number - 1), shift))
            .DistinctBy(slot => (slot.Date, slot.Shift.Id))
            .ToList();

        if (plannedSlots.Count == 0)
        {
            throw new InvalidOperationException("No roster slots can be created. Assign shifts to day groups and days first.");
        }

        var availableStaff = data.Staff.Where(staff => staff.IsAvailable).ToArray();
        if (availableStaff.Length == 0)
        {
            throw new InvalidOperationException("Add at least one available staff member before generating a roster.");
        }

        var leaveDatesByStaff = data.Leave
            .GroupBy(leave => leave.StaffId)
            .ToDictionary(group => group.Key, group => group.Select(leave => leave.Date).ToHashSet());
        var model = new CpModel();
        var assignments = new Dictionary<AssignmentKey, BoolVar>();
        var roleAssignments = new Dictionary<(AssignmentKey Assignment, Guid RoleId), BoolVar>();
        var slotsByDate = plannedSlots.GroupBy(slot => slot.Date).ToDictionary(group => group.Key, group => group.ToArray());
        var shiftsByDate = slotsByDate.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(slot => slot.Shift.Id).ToHashSet());

        foreach (var slot in plannedSlots)
        {
            foreach (var staff in availableStaff)
            {
                var key = new AssignmentKey(staff.Id, slot.Date, slot.Shift.Id);
                var assigned = model.NewBoolVar($"assigned_{staff.Id:N}_{slot.Date:yyyyMMdd}_{slot.Shift.Id:N}");
                assignments.Add(key, assigned);

                if (leaveDatesByStaff.TryGetValue(staff.Id, out var leaveDates) && leaveDates.Contains(slot.Date))
                {
                    model.Add(assigned == 0);
                }

                foreach (var roleId in staff.RoleIds.Distinct())
                {
                    var roleAssigned = model.NewBoolVar($"role_{staff.Id:N}_{slot.Date:yyyyMMdd}_{slot.Shift.Id:N}_{roleId:N}");
                    roleAssignments.Add((key, roleId), roleAssigned);
                }

                var roleVars = staff.RoleIds.Distinct()
                    .Where(roleId => roleAssignments.ContainsKey((key, roleId)))
                    .Select(roleId => (LinearExpr)roleAssignments[(key, roleId)])
                    .ToArray();
                if (roleVars.Length == 0)
                {
                    model.Add(assigned == 0);
                }
                else
                {
                    model.Add(LinearExpr.Sum(roleVars) == assigned);
                }
            }
        }

        AddSkillMixConstraints(data, model, plannedSlots, availableStaff, assignments, roleAssignments);
        AddStaffConstraints(data, model, startDate, endDate, periodLength, availableStaff, assignments, shiftsByDate, leaveDatesByStaff);
        AddShiftSequenceConstraints(data, model, startDate, endDate, periodLength, groupDays, assignments, shiftsByDate);
        AddRequestObjective(data, model, assignments);

        var solver = new CpSolver { StringParameters = "max_time_in_seconds:120" };
        var status = solver.Solve(model);
        if (status is not CpSolverStatus.Optimal and not CpSolverStatus.Feasible)
        {
            throw new InvalidOperationException(
                $"Roster generation did not find a feasible assignment (solver status: {status}). Review coverage, leave, staff targets, and sequence rules.");
        }

        var generatedSlots = plannedSlots.Select(slot =>
        {
            var assignedStaff = availableStaff
                .Where(staff => solver.BooleanValue(assignments[new AssignmentKey(staff.Id, slot.Date, slot.Shift.Id)]))
                .Select(staff => staff.Id)
                .ToList();
            return new ShiftSlot { Date = slot.Date, ShiftId = slot.Shift.Id, StaffIds = assignedStaff };
        }).ToArray();

        var periodEndExclusive = endDate.AddDays(1);
        data.Slots.RemoveAll(slot => slot.Date >= startDate && slot.Date < periodEndExclusive);
        data.Slots.AddRange(generatedSlots);
        return new GenerationResult(startDate, endDate, status.ToString(), checked((long)solver.ObjectiveValue));
    }

    private static void AddSkillMixConstraints(
        RosterData data,
        CpModel model,
        IReadOnlyCollection<PlannedSlot> slots,
        IReadOnlyCollection<StaffMember> staff,
        IReadOnlyDictionary<AssignmentKey, BoolVar> assignments,
        IReadOnlyDictionary<(AssignmentKey Assignment, Guid RoleId), BoolVar> roleAssignments)
    {
        var rulesByShift = data.SkillMixRules.GroupBy(rule => rule.ShiftId).ToDictionary(group => group.Key, group => group.ToArray());
        var requirementsByRule = data.SkillMixRequirements.GroupBy(item => item.SkillMixRuleId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        foreach (var slot in slots)
        {
            if (!rulesByShift.TryGetValue(slot.Shift.Id, out var rules) || rules.Length == 0)
            {
                continue;
            }

            var ruleChoices = rules.Select(rule => (Rule: rule, Choice: model.NewBoolVar($"rule_{rule.Id:N}_{slot.Date:yyyyMMdd}"))).ToArray();
            model.Add(LinearExpr.Sum(ruleChoices.Select(pair => (LinearExpr)pair.Choice).ToArray()) == 1);

            foreach (var choice in ruleChoices)
            {
                if (!requirementsByRule.TryGetValue(choice.Rule.Id, out var requirements))
                {
                    continue;
                }

                foreach (var requirement in requirements)
                {
                    var roleVars = staff
                        .Select(member => roleAssignments.TryGetValue(
                            (new AssignmentKey(member.Id, slot.Date, slot.Shift.Id), requirement.RoleId),
                            out var variable) ? (LinearExpr)variable : LinearExpr.Constant(0))
                        .ToArray();
                    model.Add(LinearExpr.Sum(roleVars) == requirement.Count).OnlyEnforceIf(choice.Choice);
                }
            }
        }
    }

    private static void AddStaffConstraints(
        RosterData data,
        CpModel model,
        DateOnly startDate,
        DateOnly endDate,
        int periodLength,
        IReadOnlyCollection<StaffMember> staff,
        IReadOnlyDictionary<AssignmentKey, BoolVar> assignments,
        IReadOnlyDictionary<DateOnly, HashSet<Guid>> shiftsByDate,
        IReadOnlyDictionary<Guid, HashSet<DateOnly>> leaveDatesByStaff)
    {
        foreach (var member in staff)
        {
            leaveDatesByStaff.TryGetValue(member.Id, out var leaveDates);
            leaveDates ??= [];
            var rosterDates = Enumerable.Range(0, periodLength)
                .Select(offset => startDate.AddDays(offset))
                .ToArray();
            var availableDates = rosterDates.Where(date => !leaveDates.Contains(date)).ToArray();

            if (member.EnforceShiftsPerRoster)
            {
                var adjustedTarget = member.ShiftsPerRoster * availableDates.Length / (double)periodLength;
                var target = member.MaxShifts ? (int)Math.Ceiling(adjustedTarget) : (int)Math.Floor(adjustedTarget);
                var allAssignments = AssignmentVars(assignments, shiftsByDate, member.Id, availableDates);
                model.Add(LinearExpr.Sum(allAssignments) == target);

                var firstHalfCount = (int)Math.Floor(target / 2d);
                var firstHalfDates = availableDates.Take(availableDates.Length / 2).ToArray();
                model.Add(LinearExpr.Sum(AssignmentVars(assignments, shiftsByDate, member.Id, firstHalfDates)) == firstHalfCount);
            }

            if (member.EnforceOneShiftPerDay)
            {
                foreach (var date in rosterDates)
                {
                    model.Add(LinearExpr.Sum(AssignmentVars(assignments, shiftsByDate, member.Id, [date])) <= 1);
                }
            }
        }
    }

    private static void AddShiftSequenceConstraints(
        RosterData data,
        CpModel model,
        DateOnly startDate,
        DateOnly endDate,
        int periodLength,
        IReadOnlyDictionary<Guid, HashSet<int>> groupDays,
        IReadOnlyDictionary<AssignmentKey, BoolVar> assignments,
        IReadOnlyDictionary<DateOnly, HashSet<Guid>> shiftsByDate)
    {
        foreach (var sequence in data.ShiftSequences.Where(item => item.DayGroupId.HasValue && item.StaffIds.Count > 0))
        {
            if (!groupDays.TryGetValue(sequence.DayGroupId!.Value, out var dayNumbers))
            {
                continue;
            }

            var positions = data.ShiftSequenceShifts
                .Where(item => item.ShiftSequenceId == sequence.Id)
                .OrderBy(item => item.Position)
                .ToArray();
            if (positions.Length == 0)
            {
                continue;
            }

            foreach (var staffId in sequence.StaffIds)
            {
                var historyStart = startDate.AddDays(-periodLength * positions.Length);
                var sequenceDates = Enumerable.Range(0, endDate.DayNumber - historyStart.DayNumber + 1)
                    .Select(offset => historyStart.AddDays(offset))
                    .Where(date => dayNumbers.Contains(DateNumber(date, startDate, periodLength)))
                    .ToArray();
                for (var startIndex = 0; startIndex + positions.Length <= sequenceDates.Length; startIndex++)
                {
                    var dates = sequenceDates.Skip(startIndex).Take(positions.Length).ToArray();
                    if (!dates.Any(date => date >= startDate && date <= endDate))
                    {
                        continue;
                    }

                    var priorMismatch = false;
                    var currentMismatch = new List<ILiteral>();
                    for (var index = 0; index < positions.Length; index++)
                    {
                        var date = dates[index];
                        var expectedShift = positions[index].ShiftId;
                        if (date < startDate)
                        {
                            var actual = dataSlotsAssignedTo(data, staffId, date);
                            var matches = expectedShift.HasValue
                                ? actual.Count == 1 && actual[0] == expectedShift.Value
                                : actual.Count == 0;
                            priorMismatch |= !matches;
                            continue;
                        }

                        var possibleShifts = shiftsByDate.TryGetValue(date, out var shifts) ? shifts : [];
                        if (expectedShift.HasValue)
                        {
                            var key = new AssignmentKey(staffId, date, expectedShift.Value);
                            if (assignments.TryGetValue(key, out var expectedAssignment))
                            {
                                currentMismatch.Add(expectedAssignment.Not());
                            }
                            else
                            {
                                priorMismatch = true;
                            }
                        }
                        else
                        {
                            var daily = possibleShifts
                                .Select(shiftId => assignments.TryGetValue(new AssignmentKey(staffId, date, shiftId), out var variable) ? variable : null)
                                .Where(variable => variable is not null)
                                .Cast<BoolVar>()
                                .ToArray();
                            if (daily.Length == 0)
                            {
                                continue;
                            }

                            var off = model.NewBoolVar($"off_{staffId:N}_{date:yyyyMMdd}_{sequence.Id:N}");
                            var count = LinearExpr.Sum(daily.Select(variable => (LinearExpr)variable).ToArray());
                            model.Add(count == 0).OnlyEnforceIf(off);
                            model.Add(count >= 1).OnlyEnforceIf(off.Not());
                            currentMismatch.Add(off.Not());
                        }
                    }

                    if (!priorMismatch && currentMismatch.Count > 0)
                    {
                        model.AddBoolOr(currentMismatch);
                    }
                }
            }
        }
    }

    private static void AddRequestObjective(
        RosterData data,
        CpModel model,
        IReadOnlyDictionary<AssignmentKey, BoolVar> assignments)
    {
        var weightedAssignments = new List<LinearExpr>();
        foreach (var request in data.Requests)
        {
            if (assignments.TryGetValue(new AssignmentKey(request.StaffId, request.Date, request.ShiftId), out var assignment))
            {
                var weight = request.Like ? request.Priority : -request.Priority;
                weightedAssignments.Add(weight * assignment);
            }
        }

        model.Maximize(LinearExpr.Sum(weightedAssignments.ToArray()));
    }

    private static LinearExpr[] AssignmentVars(
        IReadOnlyDictionary<AssignmentKey, BoolVar> assignments,
        IReadOnlyDictionary<DateOnly, HashSet<Guid>> shiftsByDate,
        Guid staffId,
        IEnumerable<DateOnly> dates)
    {
        return dates
            .Where(shiftsByDate.ContainsKey)
            .SelectMany(date => shiftsByDate[date].Select(shiftId =>
                assignments.TryGetValue(new AssignmentKey(staffId, date, shiftId), out var variable)
                    ? (LinearExpr)variable
                    : LinearExpr.Constant(0)))
            .ToArray();
    }

    private static int DateNumber(DateOnly date, DateOnly periodStart, int periodLength)
    {
        var offset = (date.DayNumber - periodStart.DayNumber) % periodLength;
        if (offset < 0)
        {
            offset += periodLength;
        }

        return offset + 1;
    }

    private static List<Guid> dataSlotsAssignedTo(RosterData data, Guid staffId, DateOnly date)
    {
        return data.Slots
            .Where(slot => slot.Date == date && slot.StaffIds.Contains(staffId))
            .Select(slot => slot.ShiftId)
            .ToList();
    }
}
