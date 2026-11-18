#include "adapters/sqlite_repo.hpp"
#include "core/greedy.hpp"

#include <gtest/gtest.h>

namespace roster {
TEST(SqliteRepo, SeedsAndPersistsRunAndAssignments) {
    auto repository = SqliteRepo::open(":memory:");
    ASSERT_TRUE(repository);
    auto staff = repository.value()->load_staff();
    ASSERT_TRUE(staff);
    ASSERT_EQ(staff->size(), 18U);
    EXPECT_EQ(staff->front().name, "Ana");
    EXPECT_EQ(staff->front().skill, Skill::SeniorRN);
    EXPECT_EQ(repository.value()->load_staff()->size(), 18U);

    const Day today{std::chrono::year{2026} / 1 / 2};
    EXPECT_EQ(*repository.value()->next_window_start(today), today);
    auto plan = greedy(5, staff.value());
    ASSERT_TRUE(plan);
    const auto end = today + std::chrono::days{2};
    RunData data{
        {today, end, 2, false, std::chrono::system_clock::now()},
        {{today, plan.value()}, {today + std::chrono::days{1}, plan.value()}, {end, plan.value()}}};
    ASSERT_TRUE(repository.value()->save_run(data));
    EXPECT_EQ(*repository.value()->next_window_start(today), end + std::chrono::days{1});
}
} // namespace roster
