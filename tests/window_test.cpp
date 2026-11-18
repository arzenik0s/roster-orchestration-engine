#include "core/agent.hpp"

#include <gtest/gtest.h>

namespace roster {
TEST(Window, AddsDaysAcrossMonthAndLeapDay) {
    const Day start{std::chrono::year{2024} / 2 / 28};
    EXPECT_EQ(std::chrono::year_month_day{add_days(start, std::chrono::days{1})},
              std::chrono::year{2024} / 2 / 29);
    EXPECT_EQ(std::chrono::year_month_day{add_days(start, std::chrono::days{2})},
              std::chrono::year{2024} / 3 / 1);
}
} // namespace roster
