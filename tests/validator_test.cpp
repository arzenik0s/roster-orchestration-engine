#include "core/greedy.hpp"
#include "core/validator.hpp"

#include <gtest/gtest.h>
#include <rapidcheck/gtest.h>

#include <cstdint>
#include <vector>

namespace roster {
namespace {
std::vector<Staff> sample_staff() {
    std::vector<Staff> staff;
    staff.reserve(18);
    for (std::int64_t i = 0; i < 18; ++i) {
        const auto skill =
            i < 3 ? Skill::SeniorRN : (i < 9 ? Skill::RN : (i < 14 ? Skill::EN : Skill::HCA));
        staff.push_back(Staff{StaffId{i + 1}, "staff", skill});
    }
    return staff;
}

DayPlan valid_plan() {
    return DayPlan{(std::uint64_t{1} << 0U) | (std::uint64_t{1} << 3U) | (std::uint64_t{1} << 9U) |
                       (std::uint64_t{1} << 10U) | (std::uint64_t{1} << 11U),
                   (std::uint64_t{1} << 1U) | (std::uint64_t{1} << 4U) | (std::uint64_t{1} << 12U) |
                       (std::uint64_t{1} << 14U),
                   (std::uint64_t{1} << 2U) | (std::uint64_t{1} << 5U) | (std::uint64_t{1} << 15U)};
}
} // namespace

TEST(Validator, AcceptsAllRequiredCapacityAndSkillRules) {
    const auto staff = sample_staff();
    EXPECT_EQ(validate(valid_plan(), rn_mask(staff), skill_mask(staff, Skill::SeniorRN)), None);
}

TEST(Validator, ReportsCapacitySkillAndDuplicateViolations) {
    const auto staff = sample_staff();
    auto plan = valid_plan();
    plan[slot_index(Slot::AM)] &= ~(std::uint64_t{1} << 0U);
    plan[slot_index(Slot::Night)] &= ~(std::uint64_t{1} << 2U);
    plan[slot_index(Slot::Night)] |= std::uint64_t{1} << 9U;
    plan[slot_index(Slot::PM)] |= std::uint64_t{1} << 3U;
    const auto violations = validate(plan, rn_mask(staff), skill_mask(staff, Skill::SeniorRN));
    EXPECT_NE(violations & AMCount, 0);
    EXPECT_NE(violations & NightNoSenior, 0);
    EXPECT_NE(violations & DuplicateStaff, 0);
}

TEST(Validator, IsolatesMissingRnAndMissingNightSenior) {
    const auto staff = sample_staff();
    auto plan = valid_plan();
    plan[slot_index(Slot::AM)] &= ~((std::uint64_t{1} << 0U) | (std::uint64_t{1} << 3U));
    plan[slot_index(Slot::AM)] |= (std::uint64_t{1} << 13U) | (std::uint64_t{1} << 16U);
    auto violations = validate(plan, rn_mask(staff), skill_mask(staff, Skill::SeniorRN));
    EXPECT_EQ(violations, AMNoRN);

    plan = valid_plan();
    plan[slot_index(Slot::Night)] &= ~((std::uint64_t{1} << 2U) | (std::uint64_t{1} << 5U));
    plan[slot_index(Slot::Night)] |= (std::uint64_t{1} << 13U) | (std::uint64_t{1} << 16U);
    violations = validate(plan, rn_mask(staff), skill_mask(staff, Skill::SeniorRN));
    EXPECT_EQ(violations, NightNoRN | NightNoSenior);
}

TEST(Validator, EmptyShiftsReportEveryCapacityAndCoverageViolation) {
    const auto staff = sample_staff();
    const auto violations = validate({}, rn_mask(staff), skill_mask(staff, Skill::SeniorRN));
    EXPECT_EQ(violations,
              AMCount | PMCount | NightCount | AMNoRN | PMNoRN | NightNoRN | NightNoSenior);
}

RC_GTEST_PROP(Greedy, ProducesAValidPlanForValidStaffSets,
              (const std::vector<int> &extra_skills, std::uint64_t rotation)) {
    std::vector<Staff> staff{{StaffId{1}, "Senior", Skill::SeniorRN},
                             {StaffId{2}, "RN1", Skill::RN},
                             {StaffId{3}, "RN2", Skill::RN}};
    for (int i = 0; i < 9; ++i) {
        staff.push_back(Staff{StaffId{4 + i}, "HCA", Skill::HCA});
    }
    for (std::size_t i = 0; i < extra_skills.size() && staff.size() < kMaxStaff; ++i) {
        const auto value = static_cast<unsigned int>(extra_skills[i]);
        const auto skill = static_cast<Skill>(value % 4U);
        staff.push_back(
            Staff{StaffId{static_cast<std::int64_t>(staff.size() + 1)}, "extra", skill});
    }
    const auto result = greedy(rotation, staff);
    RC_ASSERT(result.has_value());
    RC_ASSERT(validate(result.value(), rn_mask(staff), skill_mask(staff, Skill::SeniorRN)) == None);
}
} // namespace roster
