#include "adapters/ollama_client.hpp"
#include "adapters/sqlite_repo.hpp"
#include "app/config.hpp"
#include "app/scheduler.hpp"
#include "core/agent.hpp"
#include "ports/clock.hpp"

#include <CLI/CLI.hpp>
#include <spdlog/spdlog.h>

#include <chrono>
#include <csignal>
#include <exception>
#include <filesystem>
#include <format>
#include <string>
#include <thread>

namespace {
volatile std::sig_atomic_t stop_requested = 0;

std::string format_day(roster::Day day) {
    return std::format("{:%Y-%m-%d}", std::chrono::year_month_day{day});
}

extern "C" void handle_signal(int) {
    stop_requested = 1;
}

class SystemClock final : public roster::IClock {
  public:
    [[nodiscard]] std::chrono::system_clock::time_point now() const noexcept override {
        return std::chrono::system_clock::now();
    }
    [[nodiscard]] roster::Day today() const noexcept override {
        return std::chrono::floor<std::chrono::days>(now());
    }
};

void run_tick(roster::IRosterRepo &repository, roster::ILlmClient &llm, const roster::IClock &clock,
              std::uint32_t window_days) {
    const auto total_start = std::chrono::steady_clock::now();
    auto staff = repository.load_staff();
    if (!staff) {
        spdlog::error("Unable to load staff: {}", staff.error());
        return;
    }
    auto start = repository.next_window_start(clock.today());
    if (!start) {
        spdlog::error("Unable to determine next roster window: {}", start.error());
        return;
    }
    auto result = roster::plan_window(
        start.value(), window_days, staff.value(), llm,
        [](roster::Day day, std::uint32_t attempt, std::string_view violations) {
            spdlog::warn("{} try {} failed: {}", format_day(day), attempt, violations);
        });
    if (!result) {
        spdlog::error("Unable to plan roster window: {}", result.error());
        return;
    }
    const auto end = roster::add_days(start.value(), std::chrono::days{window_days - 1});
    const roster::RunData run{
        {start.value(), end, result->attempts, result->used_fallback, clock.now()}, result->days};
    auto saved = repository.save_run(run);
    if (!saved) {
        spdlog::error("Unable to save roster: {}", saved.error());
        return;
    }
    const auto total = std::chrono::steady_clock::now() - total_start;
    spdlog::info("Roster updated: {}..{}, LLM attempts {}, fallback {}, LLM {:.2f}s, total {:.2f}s",
                 format_day(start.value()), format_day(end), result->attempts,
                 result->used_fallback, std::chrono::duration<double>(result->llm_time).count(),
                 std::chrono::duration<double>(total).count());
}
} // namespace

int main(int argc, char **argv) {
    CLI::App app{"Local-first ward roster planner"};
    std::string config_path{"config.toml"};
    app.add_option("-c,--config", config_path, "TOML configuration file");
    try {
        app.parse(argc, argv);
    } catch (const CLI::ParseError &error) {
        return app.exit(error);
    }

    auto config = roster::load_config(config_path);
    if (!config) {
        spdlog::critical("Configuration error: {}", config.error());
        return 2;
    }
    const std::filesystem::path database_path{config->database};
    if (!database_path.parent_path().empty()) {
        std::error_code error;
        std::filesystem::create_directories(database_path.parent_path(), error);
        if (error) {
            spdlog::critical("Unable to create database directory: {}", error.message());
            return 2;
        }
    }
    auto repository = roster::SqliteRepo::open(config->database);
    if (!repository) {
        spdlog::critical("Database error: {}", repository.error());
        return 2;
    }
    roster::OllamaClient llm(config->ollama);
    SystemClock clock;
    roster::Scheduler scheduler(config->interval, [&] {
        try {
            run_tick(**repository, llm, clock, config->window_days);
        } catch (const std::exception &error) {
            spdlog::error("Roster tick failed: {}", error.what());
        }
    });

    std::signal(SIGINT, handle_signal);
    std::signal(SIGTERM, handle_signal);
    std::jthread worker([&](std::stop_token token) { scheduler.run(token); });
    while (stop_requested == 0) {
        std::this_thread::sleep_for(std::chrono::milliseconds{100});
    }
    worker.request_stop();
    return 0;
}
