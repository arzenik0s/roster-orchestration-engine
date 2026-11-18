#pragma once

#include "core/types.hpp"
#include "core/validator.hpp"
#include "ports/expected.hpp"
#include "ports/llm_client.hpp"

#include <chrono>
#include <cstdint>
#include <functional>
#include <string>
#include <string_view>
#include <vector>

namespace roster {

struct AgentResult {
    std::vector<RosterDay> days;
    std::uint32_t attempts{};
    bool used_fallback{};
    std::chrono::steady_clock::duration llm_time{};
};

[[nodiscard]] Expected<AgentResult, std::string>
plan_window(Day start, std::uint32_t window_days, const std::vector<Staff> &staff, ILlmClient &llm,
            const std::function<void(Day, std::uint32_t, std::string_view)> &on_retry = {});
[[nodiscard]] std::string format_violations(std::uint16_t violations);
[[nodiscard]] std::string build_prompt(Day day, const std::vector<Staff> &staff,
                                       std::uint16_t violations, std::string_view last_error = {});
[[nodiscard]] Day add_days(Day date, std::chrono::days count) noexcept;

} // namespace roster
