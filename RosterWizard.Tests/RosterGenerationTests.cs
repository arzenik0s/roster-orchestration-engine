using Kronos.RosterWizard.Application.Services;
using Kronos.RosterWizard.Domain;

namespace Kronos.RosterWizard.Tests;

[TestClass]
public sealed class RosterGenerationTests
{
    [TestMethod]
    public void Generate_CreatesExactSkillMix_HonorsLeaveAndWeightedRequest()
    {
        var data = CreateRosterData();
        var startDate = new DateOnly(2026, 1, 5);
        var nurseOne = data.Staff[0];
        var nurseTwo = data.Staff[1];
        var assistant = data.Staff[2];
        var shift = data.Shifts[0];
        data.Leave.Add(new LeaveEntry { StaffId = nurseOne.Id, Date = startDate.AddDays(1) });
        data.Requests.Add(new StaffRequest
        {
            StaffId = nurseTwo.Id,
            Date = startDate,
            ShiftId = shift.Id,
            Like = true,
            Priority = 5
        });
        data.Slots.Add(new ShiftSlot
        {
            Date = startDate.AddDays(-1),
            ShiftId = shift.Id,
            StaffIds = [nurseOne.Id]
        });

        var result = new RosterGenerationService().Generate(data, startDate);

        Assert.AreEqual(startDate, result.StartDate);
        Assert.AreEqual(startDate.AddDays(1), result.EndDate);
        Assert.AreEqual(2, data.Slots.Count(slot => slot.Date >= startDate && slot.Date <= result.EndDate));
        Assert.AreEqual(1, data.Slots.Count(slot => slot.Date == startDate.AddDays(-1)));
        var firstDay = data.Slots.Single(slot => slot.Date == startDate);
        var secondDay = data.Slots.Single(slot => slot.Date == startDate.AddDays(1));
        Assert.Contains(nurseTwo.Id, firstDay.StaffIds);
        Assert.DoesNotContain(nurseOne.Id, secondDay.StaffIds);
        Assert.HasCount(2, firstDay.StaffIds);
        Assert.HasCount(2, secondDay.StaffIds);
        Assert.IsTrue(data.Slots.All(slot => slot.ShiftId == shift.Id));
        Assert.IsGreaterThanOrEqualTo(result.RequestScore, 5L);
        Assert.IsTrue(data.Slots.Where(slot => slot.Date >= startDate && slot.Date <= result.EndDate)
            .All(slot => slot.StaffIds.Contains(assistant.Id)));
    }

    [TestMethod]
    public void Validate_RejectsNonConsecutiveRosterDays()
    {
        var data = CreateRosterData();
        data.Days[1].Number = 3;

        Assert.ThrowsExactly<InvalidDataException>(() => RosterDataValidator.Validate(data));
    }

    [TestMethod]
    public void Validate_RejectsDuplicateDayGroupMembership()
    {
        var data = CreateRosterData();
        data.DayGroupDays.Add(new DayGroupDay
        {
            DayGroupId = data.DayGroupDays[0].DayGroupId,
            DayId = data.DayGroupDays[0].DayId
        });

        Assert.ThrowsExactly<InvalidDataException>(() => RosterDataValidator.Validate(data));
    }

    private static RosterData CreateRosterData()
    {
        var nurseRole = new Role { RoleName = "Nurse" };
        var assistantRole = new Role { RoleName = "Assistant" };
        var group = new DayGroup { Name = "All days" };
        var days = new[] { new RosterDay { Number = 1 }, new RosterDay { Number = 2 } };
        var shift = new ShiftDefinition { ShiftType = "Day", DayGroupId = group.Id };
        var rule = new SkillMixRule { Name = "Standard", ShiftId = shift.Id };
        return new RosterData
        {
            Roles = [nurseRole, assistantRole],
            DayGroups = [group],
            Days = [.. days],
            DayGroupDays = days.Select(day => new DayGroupDay { DayGroupId = group.Id, DayId = day.Id }).ToList(),
            Shifts = [shift],
            Staff =
            [
                new StaffMember { FirstName = "Nurse", LastName = "One", RoleIds = [nurseRole.Id], EnforceShiftsPerRoster = false },
                new StaffMember { FirstName = "Nurse", LastName = "Two", RoleIds = [nurseRole.Id], EnforceShiftsPerRoster = false },
                new StaffMember { FirstName = "Care", LastName = "Assistant", RoleIds = [assistantRole.Id], EnforceShiftsPerRoster = false }
            ],
            SkillMixRules = [rule],
            SkillMixRequirements =
            [
                new SkillMixRequirement { SkillMixRuleId = rule.Id, RoleId = nurseRole.Id, Count = 1 },
                new SkillMixRequirement { SkillMixRuleId = rule.Id, RoleId = assistantRole.Id, Count = 1 }
            ]
        };
    }
}
