#include "app/config.hpp"

#include <gtest/gtest.h>

namespace roster {
TEST(Config, ParsesDemoAndRealIntervals) {
    auto demo = parse_config("[Roster]\nInterval=\"00:02:00\"\nWindowDays=3\n"
                             "[Ollama]\nUrl=\"http://localhost:11434\"\nModel=\"llama3.2:3b\"\n");
    ASSERT_TRUE(demo);
    EXPECT_EQ(demo->interval, std::chrono::minutes{2});
    EXPECT_EQ(demo->window_days, 3U);

    auto real = parse_config("[Roster]\nInterval=\"14.00:00:00\"\nWindowDays=14\n");
    ASSERT_TRUE(real);
    EXPECT_EQ(real->interval, std::chrono::hours{24 * 14});
    EXPECT_EQ(real->window_days, 14U);
}

TEST(Config, RejectsInvalidValues) {
    EXPECT_FALSE(parse_config("[Roster]\nInterval=\"not-a-time\"\n"));
    EXPECT_FALSE(parse_config("[Roster]\nWindowDays=0\n"));
    EXPECT_FALSE(parse_config("[Ollama]\nTemperature=3.0\n"));
    EXPECT_FALSE(parse_config("[Roster]\nWindowDays=\"three\"\n"));
}
} // namespace roster
