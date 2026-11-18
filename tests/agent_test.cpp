#include "core/agent.hpp"
#include "core/greedy.hpp"

#include <gtest/gtest.h>

#include <deque>
#include <string>
#include <utility>

namespace roster {
namespace {
class FakeLlm final : public ILlmClient {
  public:
    explicit FakeLlm(std::deque<Expected<DayPlan, LlmError>> responses)
        : responses_(std::move(responses)) {}
    Expected<DayPlan, LlmError> propose(std::string_view prompt,
                                        const std::vector<Staff> &) override {
        prompts.emplace_back(prompt);
        auto value = std::move(responses_.front());
        responses_.pop_front();
        return value;
    }
    std::vector<std::string> prompts;

  private:
    std::deque<Expected<DayPlan, LlmError>> responses_;
};

std::vector<Staff> sample_staff() {
    std::vector<Staff> staff{{StaffId{1}, "Senior", Skill::SeniorRN},
                             {StaffId{2}, "RN1", Skill::RN},
                             {StaffId{3}, "RN2", Skill::RN}};
    for (std::int64_t i = 4; i <= 18; ++i) {
        staff.push_back(Staff{StaffId{i}, "HCA", Skill::HCA});
    }
    return staff;
}
} // namespace

TEST(Agent, RetriesBadJsonAndWrongCountsBeforeAcceptingValidPlan) {
    const auto staff = sample_staff();
    auto valid = greedy(0, staff);
    ASSERT_TRUE(valid);
    FakeLlm llm({unexpected(LlmError{"invalid JSON"}), DayPlan{}, valid.value()});
    const auto start = Day{std::chrono::year{2026} / 1 / 1};
    auto result = plan_window(start, 1, staff, llm);
    ASSERT_TRUE(result);
    EXPECT_EQ(result->attempts, 3U);
    EXPECT_FALSE(result->used_fallback);
    ASSERT_EQ(llm.prompts.size(), 3U);
    EXPECT_NE(llm.prompts[1].find("Reply was not valid JSON"), std::string::npos);
    EXPECT_NE(llm.prompts[2].find("exactly"), std::string::npos);
}

TEST(Agent, UsesFallbackAfterThreeInvalidResponses) {
    const auto staff = sample_staff();
    FakeLlm llm({unexpected(LlmError{"bad"}), DayPlan{}, DayPlan{}});
    const auto start = Day{std::chrono::year{2026} / 1 / 1};
    auto result = plan_window(start, 1, staff, llm);
    ASSERT_TRUE(result);
    EXPECT_EQ(result->attempts, 3U);
    EXPECT_TRUE(result->used_fallback);
    ASSERT_EQ(result->days.size(), 1U);
    EXPECT_EQ(
        validate(result->days.front().second, rn_mask(staff), skill_mask(staff, Skill::SeniorRN)),
        None);
}
} // namespace roster
