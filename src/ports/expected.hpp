#pragma once

#include <type_traits>
#include <utility>

#if __has_include(<expected>)
#include <expected>
#endif

#if defined(__cpp_lib_expected) && __cpp_lib_expected >= 202202L
namespace roster {
template <typename T, typename E> using Expected = std::expected<T, E>;
template <typename E> using Unexpected = std::unexpected<E>;
template <typename E> [[nodiscard]] auto unexpected(E &&error) {
    return std::unexpected<std::decay_t<E>>(std::forward<E>(error));
}
} // namespace roster
#else
#include <tl/expected.hpp>
namespace roster {
template <typename T, typename E> using Expected = tl::expected<T, E>;
template <typename E> using Unexpected = tl::unexpected<E>;
template <typename E> [[nodiscard]] auto unexpected(E &&error) {
    return tl::make_unexpected(std::forward<E>(error));
}
} // namespace roster
#endif
