#pragma once

#include "core/types.hpp"

#include <array>
#include <bit>
#include <cstdint>

namespace roster {

enum Violation : std::uint16_t {
    None = 0,
    AMCount = 1U << 0U,
    PMCount = 1U << 1U,
    NightCount = 1U << 2U,
    AMNoRN = 1U << 3U,
    PMNoRN = 1U << 4U,
    NightNoRN = 1U << 5U,
    NightNoSenior = 1U << 6U,
    DuplicateStaff = 1U << 7U
};

[[nodiscard]] constexpr std::uint16_t validate(const DayPlan &plan, std::uint64_t rn_mask,
                                               std::uint64_t senior_mask) noexcept {
    std::uint16_t violations = None;
    constexpr std::array<std::uint16_t, 3> count_bits{AMCount, PMCount, NightCount};
    constexpr std::array<std::uint16_t, 3> rn_bits{AMNoRN, PMNoRN, NightNoRN};
    std::uint64_t seen = 0;

    for (std::size_t i = 0; i < plan.size(); ++i) {
        const auto mask = plan[i];
        if (static_cast<std::uint32_t>(std::popcount(mask)) != kCapacities[i]) {
            violations |= count_bits[i];
        }
        if ((mask & rn_mask) == 0) {
            violations |= rn_bits[i];
        }
        if ((seen & mask) != 0) {
            violations |= DuplicateStaff;
        }
        seen |= mask;
    }
    if ((plan[slot_index(Slot::Night)] & senior_mask) == 0) {
        violations |= NightNoSenior;
    }
    return violations;
}

} // namespace roster
