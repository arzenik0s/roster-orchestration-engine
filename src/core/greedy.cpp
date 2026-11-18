#include "core/greedy.hpp"

#include "core/validator.hpp"

#include <bit>

namespace roster {
std::uint64_t skill_mask(const std::vector<Staff> &staff, Skill skill) noexcept {
    std::uint64_t mask = 0;
    for (std::size_t i = 0; i < staff.size() && i < kMaxStaff; ++i) {
        if (staff[i].skill == skill) {
            mask |= std::uint64_t{1} << i;
        }
    }
    return mask;
}

std::uint64_t rn_mask(const std::vector<Staff> &staff) noexcept {
    return skill_mask(staff, Skill::RN) | skill_mask(staff, Skill::SeniorRN);
}

Expected<DayPlan, std::string> greedy(std::uint64_t rotation, const std::vector<Staff> &staff) {
    if (staff.size() < 12 || staff.size() > kMaxStaff) {
        return unexpected(std::string("Greedy roster requires 12-64 staff members"));
    }
    const auto offset = static_cast<std::size_t>(rotation % staff.size());
    DayPlan plan{};
    std::vector<bool> used(staff.size(), false);
    for (const Slot slot : {Slot::Night, Slot::PM, Slot::AM}) {
        const auto idx = slot_index(slot);
        std::uint64_t selection = 0;
        auto select_one = [&](auto predicate) {
            for (std::size_t i = 0; i < staff.size(); ++i) {
                const auto staff_index = (offset + i) % staff.size();
                if (used[staff_index] || !predicate(staff[staff_index])) {
                    continue;
                }
                used[staff_index] = true;
                selection |= std::uint64_t{1} << staff_index;
                return true;
            }
            return false;
        };
        if (slot == Slot::Night &&
            !select_one([](const Staff &s) { return s.skill == Skill::SeniorRN; })) {
            return unexpected(std::string("Greedy roster requires a SeniorRN"));
        }
        if ((selection & rn_mask(staff)) == 0 && !select_one([](const Staff &s) {
                return s.skill == Skill::SeniorRN || s.skill == Skill::RN;
            })) {
            return unexpected(std::string("Greedy roster requires enough RN/SeniorRN staff"));
        }
        while (static_cast<std::uint32_t>(std::popcount(selection)) < kCapacities[idx]) {
            const auto non_rn = [](const Staff &s) {
                return s.skill != Skill::RN && s.skill != Skill::SeniorRN;
            };
            if (!select_one(non_rn) && !select_one([](const Staff &) { return true; })) {
                return unexpected(std::string("Greedy roster has insufficient available staff"));
            }
        }
        plan[idx] = selection;
    }
    return plan;
}
} // namespace roster
