#pragma once

#include "core/types.hpp"
#include "ports/expected.hpp"

#include <chrono>
#include <string>
#include <vector>

namespace roster {

struct RunRecord {
    Day window_start;
    Day window_end;
    std::uint32_t attempts{};
    bool used_fallback{};
    std::chrono::system_clock::time_point ran_at;
};

struct RunData {
    RunRecord run;
    std::vector<RosterDay> days;
};

class IRosterRepo {
  public:
    virtual ~IRosterRepo() = default;
    [[nodiscard]] virtual Expected<std::vector<Staff>, std::string> load_staff() = 0;
    [[nodiscard]] virtual Expected<Day, std::string> next_window_start(Day today) = 0;
    [[nodiscard]] virtual Expected<void, std::string> save_run(const RunData &run) = 0;
    [[nodiscard]] virtual std::expected<void, RepoError> ExportAssignmentsCsv(const std::string& path) = 0;
};

} // namespace roster
