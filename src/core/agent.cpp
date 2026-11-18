#include "core/agent.hpp"

#include "core/greedy.hpp"

#include <algorithm>
#include <array>
#include <chrono>
#include <format>
#include <sstream>

namespace roster {
namespace {
constexpr std::array<std::uint16_t, 3> kCountViolations{AMCount, PMCount, NightCount};
constexpr std::array<std::uint16_t, 3> kRnViolations{AMNoRN, PMNoRN, NightNoRN};
constexpr std::array<std::string_view, 3> kSlotNames{"AM", "PM", "Night"};
constexpr std::string_view kExample = R"({"AM":[1,2,3,4,5],"PM":[6,7,8,9],"Night":[10,11,12]})";
} // namespace

Day add_days(Day date, std::chrono::days count) noexcept {
    return date + count;
}

std::string format_violations(std::uint16_t violations) {
    std::ostringstream text;
    bool first = true;
    auto append = [&](std::string_view message) {
        if (!first) {
            text << "; ";
        }
        text << message;
        first = false;
    };
    for (std::size_t i = 0; i < kSlotNames.size(); ++i) {
        if ((violations & kCountViolations[i]) != 0) {
            append(std::format("{} must have exactly {} staff", kSlotNames[i], kCapacities[i]));
        }
        if ((violations & kRnViolations[i]) != 0) {
            append(std::format("{} needs at least one RN or SeniorRN", kSlotNames[i]));
        }
    }
    if ((violations & NightNoSenior) != 0) {
        append("Night needs at least one SeniorRN");
    }
    if ((violations & DuplicateStaff) != 0) {
        append("A staff member cannot work more than one shift today");
    }
    return text.str();
}

std::string build_prompt(Day day, const std::vector<Staff> &staff, std::uint16_t violations,
                         std::string_view last_error) {
    auto ordered = staff;
    const auto day_number = day.time_since_epoch().count();
    std::ranges::stable_sort(ordered, [day_number](const Staff &left, const Staff &right) {
        const auto left_order = (left.id.value * 7 + day_number) % 13;
        const auto right_order = (right.id.value * 7 + day_number) % 13;
        return left_order < right_order;
    });

    const std::chrono::year_month_day date{day};
    std::ostringstream prompt;
    prompt << "You are rostering one hospital ward for " << std::format("{:%Y-%m-%d}", date)
           << ".\n"
           << "Staff (id | name | skill):\n";
    for (const auto &member : ordered) {
        const std::array<std::string_view, 4> names{"SeniorRN", "RN", "EN", "HCA"};
        prompt << member.id.value << " | " << member.name << " | "
               << names[static_cast<std::size_t>(member.skill)] << '\n';
    }
    prompt << "\nRules:\n"
           << "- AM needs exactly 5 staff, PM exactly 4, Night exactly 3.\n"
           << "- Every shift needs at least one RN or SeniorRN.\n"
           << "- Night needs at least one SeniorRN.\n"
           << "- Each person works at most ONE shift today.\n"
           << "- Rotate people fairly; don't always pick the first names in the list.\n"
           << "Reply with JSON only, using staff ids, like this: " << kExample;
    if (violations != None || !last_error.empty()) {
        prompt << "\nYour last answer broke these rules. Fix them:\n";
        const auto detail = format_violations(violations);
        if (!detail.empty()) {
            prompt << detail << '\n';
        }
        if (!last_error.empty()) {
            prompt << last_error << '\n';
        }
    }
    return prompt.str();
}

Expected<AgentResult, std::string>
plan_window(Day start, std::uint32_t window_days, const std::vector<Staff> &staff, ILlmClient &llm,
            const std::function<void(Day, std::uint32_t, std::string_view)> &on_retry) {
    if (window_days == 0) {
        return unexpected(std::string("Roster window must contain at least one day"));
    }
    if (staff.empty() || staff.size() > kMaxStaff) {
        return unexpected(std::string("Roster requires 1-64 staff members"));
    }

    AgentResult result;
    result.days.reserve(window_days);
    const auto rn = rn_mask(staff);
    const auto senior = skill_mask(staff, Skill::SeniorRN);
    for (std::uint32_t day_index = 0; day_index < window_days; ++day_index) {
        const auto day = add_days(start, std::chrono::days{day_index});
        std::uint16_t violations = None;
        std::string last_error;
        bool accepted = false;
        for (std::uint32_t attempt = 1; attempt <= 3; ++attempt) {
            const auto prompt = build_prompt(day, staff, violations, last_error);
            const auto started = std::chrono::steady_clock::now();
            auto proposal = llm.propose(prompt, staff);
            result.llm_time += std::chrono::steady_clock::now() - started;
            ++result.attempts;

            if (!proposal) {
                last_error = "Reply was not valid JSON: " + proposal.error().message;
                violations = None;
                if (on_retry) {
                    on_retry(day, attempt, last_error);
                }
                continue;
            }

            violations = validate(proposal.value(), rn, senior);
            if (violations == None) {
                result.days.emplace_back(day, proposal.value());
                accepted = true;
                break;
            }
            last_error = format_violations(violations);
            if (on_retry) {
                on_retry(day, attempt, last_error);
            }
        }
        if (!accepted) {
            auto fallback =
                greedy(static_cast<std::uint64_t>(day.time_since_epoch().count()), staff);
            if (!fallback) {
                return unexpected(fallback.error());
            }
            result.days.emplace_back(day, fallback.value());
            result.used_fallback = true;
        }
    }
    return result;
}
} // namespace roster
