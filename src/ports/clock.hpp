#pragma once

#include "core/types.hpp"

#include <chrono>

namespace roster {

class IClock {
  public:
    virtual ~IClock() = default;
    [[nodiscard]] virtual std::chrono::system_clock::time_point now() const noexcept = 0;
    [[nodiscard]] virtual Day today() const noexcept = 0;
};

} // namespace roster
