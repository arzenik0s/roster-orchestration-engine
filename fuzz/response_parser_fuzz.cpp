#include "adapters/response_parser.hpp"

#include <cstddef>
#include <cstdint>
#include <string_view>
#include <vector>

extern "C" int LLVMFuzzerTestOneInput(const std::uint8_t *data, std::size_t size) {
    const std::vector<roster::Staff> staff{{roster::StaffId{1}, "Ana", roster::Skill::SeniorRN},
                                           {roster::StaffId{2}, "Ben", roster::Skill::RN}};
    const std::string_view input{reinterpret_cast<const char *>(data), size};
    static_cast<void>(roster::parse_response(input, staff));
    return 0;
}
