#pragma once

#include "core/types.hpp"
#include "ports/expected.hpp"

#include <string>
#include <string_view>
#include <vector>

namespace roster {

struct LlmError {
    std::string message;
};

class ILlmClient {
  public:
    virtual ~ILlmClient() = default;
    [[nodiscard]] virtual Expected<DayPlan, LlmError> propose(std::string_view prompt,
                                                              const std::vector<Staff> &staff) = 0;
};

} // namespace roster
