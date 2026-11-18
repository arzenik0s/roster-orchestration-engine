#pragma once

#include "core/types.hpp"
#include "ports/expected.hpp"

#include <string>
#include <vector>

namespace roster {

[[nodiscard]] Expected<DayPlan, std::string> greedy(std::uint64_t rotation,
                                                    const std::vector<Staff> &staff);
[[nodiscard]] std::uint64_t skill_mask(const std::vector<Staff> &staff, Skill skill) noexcept;
[[nodiscard]] std::uint64_t rn_mask(const std::vector<Staff> &staff) noexcept;

} // namespace roster
