#pragma once

#include "ports/roster_repo.hpp"

#include <memory>
#include <string>
#include <vector>

struct sqlite3;
struct sqlite3_stmt;

namespace roster {

class SqliteRepo final : public IRosterRepo {
  public:
    [[nodiscard]] static Expected<std::unique_ptr<SqliteRepo>, std::string>
    open(const std::string &path);
    ~SqliteRepo() override;

    SqliteRepo(const SqliteRepo &) = delete;
    SqliteRepo &operator=(const SqliteRepo &) = delete;

    [[nodiscard]] Expected<std::vector<Staff>, std::string> load_staff() override;
    [[nodiscard]] Expected<Day, std::string> next_window_start(Day today) override;
    [[nodiscard]] Expected<void, std::string> save_run(const RunData &run) override;

  private:
    explicit SqliteRepo(sqlite3 *database) noexcept;
    [[nodiscard]] Expected<void, std::string> initialize();

    sqlite3 *database_{};
    sqlite3_stmt *staff_query_{};
    sqlite3_stmt *latest_run_query_{};
    sqlite3_stmt *assignment_insert_{};
    sqlite3_stmt *run_insert_{};
    std::vector<Staff> staff_cache_;
};

} // namespace roster
