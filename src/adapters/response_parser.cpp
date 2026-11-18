#include "adapters/response_parser.hpp"

#include <nlohmann/json.hpp>

#include <array>
#include <cstdint>
#include <unordered_map>

namespace roster {
Expected<DayPlan, std::string> parse_response(std::string_view response,
                                              const std::vector<Staff> &staff) {
    try {
        const auto json = nlohmann::json::parse(response);
        constexpr std::array<std::string_view, 3> slots{"AM", "PM", "Night"};
        if (!json.is_object() || json.size() != slots.size()) {
            return unexpected(
                std::string("Response must be an object with exactly AM, PM, and Night"));
        }

        std::unordered_map<std::int64_t, std::size_t> indexes;
        indexes.reserve(staff.size());
        for (std::size_t i = 0; i < staff.size(); ++i) {
            if (!indexes.emplace(staff[i].id.value, i).second) {
                return unexpected(std::string("Staff list contains duplicate IDs"));
            }
        }

        DayPlan plan{};
        for (std::size_t slot = 0; slot < slots.size(); ++slot) {
            const auto key = std::string(slots[slot]);
            if (!json.contains(key) || !json.at(key).is_array()) {
                return unexpected(std::string("Each slot must contain an array of staff IDs"));
            }
            for (const auto &value : json.at(key)) {
                if (!value.is_number_integer()) {
                    return unexpected(std::string("Staff IDs must be integers"));
                }
                const auto id = value.get<std::int64_t>();
                const auto found = indexes.find(id);
                if (found == indexes.end()) {
                    return unexpected(std::string("Response contains an unknown staff ID"));
                }
                const auto bit = std::uint64_t{1} << found->second;
                if ((plan[slot] & bit) != 0) {
                    return unexpected(std::string("Response repeats a staff ID within a slot"));
                }
                plan[slot] |= bit;
            }
        }
        return plan;
    } catch (const nlohmann::json::exception &error) {
        return unexpected(std::string("Invalid JSON response: ") + error.what());
    }
}
} // namespace roster
