using Kronos.RosterWizard.Domain;

namespace Kronos.RosterWizard.Infrastructure;

internal static class RosterDataSeeder
{
    public static RosterData Create()
    {
        var data = new RosterData();
        var registeredNurse = new Role { RoleName = "Registered nurse" };
        var enrolledNurse = new Role { RoleName = "Enrolled nurse" };
        var dayGroup = new DayGroup { Name = "Every day" };
        data.Roles.AddRange([registeredNurse, enrolledNurse]);
        data.DayGroups.Add(dayGroup);

        for (var number = 1; number <= 14; number++)
        {
            var day = new RosterDay { Number = number };
            data.Days.Add(day);
            data.DayGroupDays.Add(new DayGroupDay { DayGroupId = dayGroup.Id, DayId = day.Id });
        }

        var dayShift = AddShift(data, dayGroup, "Day", registeredNurse, enrolledNurse);
        var evening = AddShift(data, dayGroup, "Evening", registeredNurse, enrolledNurse);
        var night = AddShift(data, dayGroup, "Night", registeredNurse, enrolledNurse);

        for (var number = 1; number <= 3; number++)
        {
            data.Staff.Add(new StaffMember
            {
                FirstName = $"Alex {number}",
                LastName = "Morgan",
                Email = $"rn{number}@example.test",
                RoleIds = [registeredNurse.Id],
                ShiftsPerRoster = 14,
                EnforceShiftsPerRoster = true
            });
        }

        for (var number = 1; number <= 4; number++)
        {
            data.Staff.Add(new StaffMember
            {
                FirstName = $"Jordan {number}",
                LastName = "Lee",
                Email = $"en{number}@example.test",
                RoleIds = [enrolledNurse.Id],
                ShiftsPerRoster = number < 3 ? 11 : 10,
                EnforceShiftsPerRoster = true
            });
        }

        var start = DateOnly.FromDateTime(DateTime.Today);
        data.Requests.Add(new StaffRequest
        {
            StaffId = data.Staff[0].Id,
            Date = start.AddDays(1),
            ShiftId = dayShift.Id,
            Like = true,
            Priority = 5
        });
        data.Requests.Add(new StaffRequest
        {
            StaffId = data.Staff[3].Id,
            Date = start.AddDays(3),
            ShiftId = evening.Id,
            Like = false,
            Priority = 3
        });
        data.Requests.Add(new StaffRequest
        {
            StaffId = data.Staff[6].Id,
            Date = start.AddDays(6),
            ShiftId = night.Id,
            Like = true,
            Priority = 2
        });

        return data;
    }

    private static ShiftDefinition AddShift(
        RosterData data,
        DayGroup dayGroup,
        string shiftType,
        Role registeredNurse,
        Role enrolledNurse)
    {
        var shift = new ShiftDefinition { ShiftType = shiftType, DayGroupId = dayGroup.Id };
        var rule = new SkillMixRule { Name = $"{shiftType} core mix", ShiftId = shift.Id };
        data.Shifts.Add(shift);
        data.SkillMixRules.Add(rule);
        data.SkillMixRequirements.AddRange(
        [
            new SkillMixRequirement { SkillMixRuleId = rule.Id, RoleId = registeredNurse.Id, Count = 1 },
            new SkillMixRequirement { SkillMixRuleId = rule.Id, RoleId = enrolledNurse.Id, Count = 1 }
        ]);
        return shift;
    }
}
