#include "app/config.hpp"

#include <toml++/toml.hpp>

#include <charconv>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <string_view>

namespace roster {
namespace {
Expected<std::chrono::seconds, std::string> parse_interval(std::string_view text) {
    const auto parse_number = [](std::string_view value, int &number) {
        const auto result = std::from_chars(value.data(), value.data() + value.size(), number);
        return result.ec == std::errc{} && result.ptr == value.data() + value.size();
    };
    int days = 0;
    int hours = 0;
    int minutes = 0;
    int seconds = 0;
    auto time = text;
    if (const auto dot = text.find('.'); dot != std::string_view::npos) {
        if (!parse_number(text.substr(0, dot), days)) {
            return unexpected(std::string("Invalid Roster:Interval day count"));
        }
        time = text.substr(dot + 1);
    }
    const auto first = time.find(':');
    const auto second = first == std::string_view::npos ? first : time.find(':', first + 1);
    if (first == std::string_view::npos || second == std::string_view::npos ||
        !parse_number(time.substr(0, first), hours) ||
        !parse_number(time.substr(first + 1, second - first - 1), minutes) ||
        !parse_number(time.substr(second + 1), seconds) || hours > 23 || minutes > 59 ||
        seconds > 59 || days < 0 || hours < 0 || minutes < 0 || seconds < 0) {
        return unexpected(std::string("Roster:Interval must be [d.]hh:mm:ss"));
    }
    const auto result = std::chrono::days{days} + std::chrono::hours{hours} +
                        std::chrono::minutes{minutes} + std::chrono::seconds{seconds};
    if (result <= std::chrono::seconds::zero()) {
        return unexpected(std::string("Roster:Interval must be positive"));
    }
    return std::chrono::duration_cast<std::chrono::seconds>(result);
}

void override_string(const char *key, std::string &value) {
    if (const auto *override_value = std::getenv(key); override_value != nullptr) {
        value = override_value;
    }
}
} // namespace

Expected<Config, std::string> parse_config(std::string_view text) {
    Config config;
    try {
        const auto root = toml::parse(text);
        if (const auto *roster = root["Roster"].as_table()) {
            if (const auto *node = roster->get("Interval")) {
                const auto interval = node->value<std::string>();
                if (!interval) {
                    return unexpected(std::string("Roster:Interval must be a string"));
                }
                auto parsed = parse_interval(*interval);
                if (!parsed) {
                    return unexpected(parsed.error());
                }
                config.interval = parsed.value();
            }
            if (const auto *node = roster->get("WindowDays")) {
                const auto days = node->value<std::int64_t>();
                if (!days) {
                    return unexpected(std::string("Roster:WindowDays must be an integer"));
                }
                if (*days < 1 || *days > 365) {
                    return unexpected(std::string("Roster:WindowDays must be between 1 and 365"));
                }
                config.window_days = static_cast<std::uint32_t>(*days);
            }
        }
        if (const auto *ollama = root["Ollama"].as_table()) {
            if (const auto *node = ollama->get("Url")) {
                const auto url = node->value<std::string>();
                if (!url) {
                    return unexpected(std::string("Ollama:Url must be a string"));
                }
                config.ollama.url = *url;
            }
            if (const auto *node = ollama->get("Model")) {
                const auto model = node->value<std::string>();
                if (!model) {
                    return unexpected(std::string("Ollama:Model must be a string"));
                }
                config.ollama.model = *model;
            }
            if (const auto *node = ollama->get("Temperature")) {
                auto temperature = node->value<double>();
                if (!temperature) {
                    if (const auto integer = node->value<std::int64_t>()) {
                        temperature = static_cast<double>(*integer);
                    }
                }
                if (!temperature) {
                    return unexpected(std::string("Ollama:Temperature must be numeric"));
                }
                if (*temperature < 0.0 || *temperature > 2.0) {
                    return unexpected(std::string("Ollama:Temperature must be from 0 to 2"));
                }
                config.ollama.temperature = *temperature;
            }
            if (const auto *node = ollama->get("Seed")) {
                const auto seed = node->value<std::int64_t>();
                if (!seed) {
                    return unexpected(std::string("Ollama:Seed must be an integer"));
                }
                if (*seed < 0 || *seed > UINT32_MAX) {
                    return unexpected(
                        std::string("Ollama:Seed must be an unsigned 32-bit integer"));
                }
                config.ollama.seed = static_cast<std::uint32_t>(*seed);
            }
        }
        if (const auto *connection = root["ConnectionStrings"].as_table()) {
            if (const auto *node = connection->get("Db")) {
                const auto database = node->value<std::string>();
                if (!database || database->empty()) {
                    return unexpected(
                        std::string("ConnectionStrings:Db must be a non-empty string"));
                }
                config.database = *database;
            }
        }
    } catch (const toml::parse_error &error) {
        return unexpected(std::string("Invalid TOML configuration: ") +
                          std::string(error.description()));
    }

    std::string interval = std::to_string(config.interval.count());
    override_string("Roster__Interval", interval);
    if (const auto *env = std::getenv("Roster__Interval"); env != nullptr) {
        auto parsed = parse_interval(interval);
        if (!parsed) {
            return unexpected(parsed.error());
        }
        config.interval = parsed.value();
    }
    if (const auto *env = std::getenv("Roster__WindowDays"); env != nullptr) {
        std::uint32_t days = 0;
        const std::string_view value{env};
        const auto parsed = std::from_chars(value.data(), value.data() + value.size(), days);
        if (parsed.ec != std::errc{} || parsed.ptr != value.data() + value.size() || days == 0 ||
            days > 365) {
            return unexpected(std::string("Roster__WindowDays must be between 1 and 365"));
        }
        config.window_days = days;
    }
    override_string("ConnectionStrings__Db", config.database);
    override_string("Ollama__Url", config.ollama.url);
    override_string("Ollama__Model", config.ollama.model);
    if (config.database.empty() || config.ollama.url.empty() || config.ollama.model.empty()) {
        return unexpected(
            std::string("Database path, Ollama URL, and Ollama model must be non-empty"));
    }
    if (const auto *env = std::getenv("Ollama__Temperature"); env != nullptr) {
        double temperature = 0.0;
        const std::string_view value{env};
        const auto parsed = std::from_chars(value.data(), value.data() + value.size(), temperature);
        if (parsed.ec != std::errc{} || parsed.ptr != value.data() + value.size() ||
            temperature < 0.0 || temperature > 2.0) {
            return unexpected(std::string("Ollama__Temperature must be from 0 to 2"));
        }
        config.ollama.temperature = temperature;
    }
    if (const auto *env = std::getenv("Ollama__Seed"); env != nullptr) {
        std::uint32_t seed = 0;
        const std::string_view value{env};
        const auto parsed = std::from_chars(value.data(), value.data() + value.size(), seed);
        if (parsed.ec != std::errc{} || parsed.ptr != value.data() + value.size()) {
            return unexpected(std::string("Ollama__Seed must be an unsigned 32-bit integer"));
        }
        config.ollama.seed = seed;
    }
    return config;
}

Expected<Config, std::string> load_config(const std::string &path) {
    std::ifstream input(path, std::ios::binary);
    if (!input) {
        return unexpected(std::string("Unable to open configuration file: ") + path);
    }
    const std::string text{std::istreambuf_iterator<char>{input}, std::istreambuf_iterator<char>{}};
    return parse_config(text);
}
} // namespace roster
