#pragma once

#include "core/types.hpp"
#include "ports/expected.hpp"

#include <string>
#include <string_view>
#include <vector>

namespace roster {

[[nodiscard]] Expected<DayPlan, std::string> parse_response(std::string_view response,
                                                            const std::vector<Staff> &staff);

} // namespace roster
