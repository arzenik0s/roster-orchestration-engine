#pragma once

#include <array>
#include <chrono>
#include <cstdint>
#include <string>
#include <utility>

namespace roster {

enum class Skill : std::uint8_t { SeniorRN = 0, RN = 1, EN = 2, HCA = 3 };
enum class Slot : std::uint8_t { AM = 0, PM = 1, Night = 2 };

struct StaffId {
    std::int64_t value{};
    friend constexpr bool operator==(StaffId, StaffId) noexcept = default;
};

struct Staff {
    StaffId id;
    std::string name;
    Skill skill;
};

using Day = std::chrono::sys_days;
using DayPlan = std::array<std::uint64_t, 3>;
using RosterDay = std::pair<Day, DayPlan>;

inline constexpr std::array<std::uint32_t, 3> kCapacities{5, 4, 3};
inline constexpr std::size_t kMaxStaff = 64;

[[nodiscard]] constexpr std::size_t slot_index(Slot slot) noexcept {
    return static_cast<std::size_t>(slot);
}

} // namespace roster
