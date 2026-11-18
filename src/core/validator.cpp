#include "core/validator.hpp"

#include <bit>

namespace roster {
static_assert(sizeof(std::uint64_t) * 8 == kMaxStaff);
} // namespace roster
