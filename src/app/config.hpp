#pragma once

#include "adapters/ollama_client.hpp"
#include "ports/expected.hpp"

#include <chrono>
#include <cstdint>
#include <string>
#include <string_view>

namespace roster {

struct Config {
    std::chrono::seconds interval{120};
    std::uint32_t window_days{3};
    std::string database{"roster.db"};
    OllamaOptions ollama{"http://localhost:11434", "llama3.2:3b"};
};

[[nodiscard]] Expected<Config, std::string> load_config(const std::string &path);
[[nodiscard]] Expected<Config, std::string> parse_config(std::string_view text);

} // namespace roster
