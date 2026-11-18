#include "adapters/sqlite_repo.hpp"

#include <sqlite3.h>

#include <array>
#include <bit>
#include <cstdio>
#include <format>
#include <memory>
#include <stdexcept>
#include <string_view>
#include <utility>

namespace roster {
namespace {
constexpr std::array<std::string_view, 18> kNames{"Ana", "Ben",  "Cora", "Dan", "Eli",  "Fay",
                                                  "Gus", "Hana", "Ivy",  "Jon", "Kai",  "Lea",
                                                  "Max", "Nia",  "Omar", "Pia", "Quin", "Rae"};
constexpr std::array<Skill, 18> kSkills{
    Skill::SeniorRN, Skill::SeniorRN, Skill::SeniorRN, Skill::RN,  Skill::RN,  Skill::RN,
    Skill::RN,       Skill::RN,       Skill::RN,       Skill::EN,  Skill::EN,  Skill::EN,
    Skill::EN,       Skill::EN,       Skill::HCA,      Skill::HCA, Skill::HCA, Skill::HCA};

std::string sqlite_error(sqlite3 *database, std::string_view operation) {
    return std::string(operation) + ": " + sqlite3_errmsg(database);
}

bool execute(sqlite3 *database, const char *sql, std::string &error) {
    char *message = nullptr;
    if (sqlite3_exec(database, sql, nullptr, nullptr, &message) == SQLITE_OK) {
        return true;
    }
    error = message == nullptr ? sqlite3_errmsg(database) : message;
    sqlite3_free(message);
    return false;
}

Expected<Day, std::string> read_date(sqlite3_stmt *statement, int column) {
    const auto *raw = reinterpret_cast<const char *>(sqlite3_column_text(statement, column));
    if (raw == nullptr) {
        return unexpected(std::string("Database contains a NULL roster date"));
    }
    const std::string_view value{raw};
    if (value.size() != 10) {
        return unexpected(std::string("Database contains an invalid roster date"));
    }
    int year = 0;
    unsigned month = 0;
    unsigned day = 0;
    if (std::sscanf(std::string(value.substr(0, 10)).c_str(), "%4d-%2u-%2u", &year, &month, &day) !=
        3) {
        return unexpected(std::string("Database contains an invalid roster date"));
    }
    const std::chrono::year_month_day parsed{std::chrono::year{year}, std::chrono::month{month},
                                             std::chrono::day{day}};
    if (!parsed.ok()) {
        return unexpected(std::string("Database contains an invalid roster date"));
    }
    return Day{parsed};
}

} // namespace

SqliteRepo::SqliteRepo(sqlite3 *database) noexcept : database_(database) {}

Expected<std::unique_ptr<SqliteRepo>, std::string> SqliteRepo::open(const std::string &path) {
    sqlite3 *database = nullptr;
    if (sqlite3_open_v2(path.c_str(), &database,
                        SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_FULLMUTEX,
                        nullptr) != SQLITE_OK) {
        const std::string error =
            database == nullptr ? "Unable to open SQLite database" : sqlite3_errmsg(database);
        if (database != nullptr) {
            sqlite3_close(database);
        }
        return unexpected(error);
    }
    auto repository = std::unique_ptr<SqliteRepo>(new SqliteRepo(database));
    auto initialized = repository->initialize();
    if (!initialized) {
        return unexpected(initialized.error());
    }
    return repository;
}

SqliteRepo::~SqliteRepo() {
    sqlite3_finalize(staff_query_);
    sqlite3_finalize(latest_run_query_);
    sqlite3_finalize(assignment_insert_);
    sqlite3_finalize(run_insert_);
    sqlite3_close(database_);
}

Expected<void, std::string> SqliteRepo::initialize() {
    std::string error;
    if (!execute(database_, "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;", error) ||
        !execute(
            database_,
            "CREATE TABLE IF NOT EXISTS \"Staff\" ("
            "\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Staff\" PRIMARY KEY AUTOINCREMENT,"
            "\"Name\" TEXT NOT NULL,\"Skill\" INTEGER NOT NULL);"
            "CREATE TABLE IF NOT EXISTS \"Assignments\" ("
            "\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Assignments\" PRIMARY KEY AUTOINCREMENT,"
            "\"Date\" TEXT NOT NULL,\"Slot\" INTEGER NOT NULL,\"StaffId\" INTEGER NOT NULL);"
            "CREATE TABLE IF NOT EXISTS \"Runs\" ("
            "\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Runs\" PRIMARY KEY AUTOINCREMENT,"
            "\"RanAt\" TEXT NOT NULL,\"WindowStart\" TEXT NOT NULL,\"WindowEnd\" TEXT NOT NULL,"
            "\"Attempts\" INTEGER NOT NULL,\"UsedFallback\" INTEGER NOT NULL);",
            error)) {
        return unexpected(std::move(error));
    }

    sqlite3_stmt *count_statement = nullptr;
    if (sqlite3_prepare_v2(database_, "SELECT COUNT(*) FROM \"Staff\";", -1, &count_statement,
                           nullptr) != SQLITE_OK) {
        return unexpected(sqlite_error(database_, "Preparing staff seed check"));
    }
    const int count_status = sqlite3_step(count_statement);
    const auto staff_count =
        count_status == SQLITE_ROW ? sqlite3_column_int64(count_statement, 0) : -1;
    sqlite3_finalize(count_statement);
    if (count_status != SQLITE_ROW) {
        return unexpected(sqlite_error(database_, "Reading staff seed check"));
    }
    if (staff_count == 0) {
        sqlite3_stmt *insert = nullptr;
        if (sqlite3_prepare_v2(database_,
                               "INSERT INTO \"Staff\" (\"Name\",\"Skill\") VALUES (?,?);", -1,
                               &insert, nullptr) != SQLITE_OK) {
            return unexpected(sqlite_error(database_, "Preparing staff seed"));
        }
        for (std::size_t i = 0; i < kNames.size(); ++i) {
            sqlite3_bind_text(insert, 1, kNames[i].data(), static_cast<int>(kNames[i].size()),
                              SQLITE_STATIC);
            sqlite3_bind_int(insert, 2, static_cast<int>(kSkills[i]));
            if (sqlite3_step(insert) != SQLITE_DONE) {
                const auto message = sqlite_error(database_, "Seeding staff");
                sqlite3_finalize(insert);
                return unexpected(message);
            }
            sqlite3_reset(insert);
            sqlite3_clear_bindings(insert);
        }
        sqlite3_finalize(insert);
    }

    const auto prepare = [&](const char *sql, sqlite3_stmt **statement, std::string_view label) {
        return sqlite3_prepare_v2(database_, sql, -1, statement, nullptr) == SQLITE_OK ||
               (error = sqlite_error(database_, label), false);
    };
    if (!prepare("SELECT \"Id\",\"Name\",\"Skill\" FROM \"Staff\" ORDER BY \"Id\";", &staff_query_,
                 "Preparing staff query") ||
        !prepare("SELECT \"WindowEnd\" FROM \"Runs\" ORDER BY \"Id\" DESC LIMIT 1;",
                 &latest_run_query_, "Preparing latest run query") ||
        !prepare("INSERT INTO \"Assignments\" (\"Date\",\"Slot\",\"StaffId\") VALUES (?,?,?);",
                 &assignment_insert_, "Preparing assignment insert") ||
        !prepare("INSERT INTO \"Runs\" "
                 "(\"RanAt\",\"WindowStart\",\"WindowEnd\",\"Attempts\",\"UsedFallback\") "
                 "VALUES (?,?,?,?,?);",
                 &run_insert_, "Preparing run insert")) {
        return unexpected(std::move(error));
    }
    return {};
}

Expected<std::vector<Staff>, std::string> SqliteRepo::load_staff() {
    staff_cache_.clear();
    sqlite3_reset(staff_query_);
    while (true) {
        const int status = sqlite3_step(staff_query_);
        if (status == SQLITE_DONE) {
            break;
        }
        if (status != SQLITE_ROW) {
            const auto message = sqlite_error(database_, "Loading staff");
            sqlite3_reset(staff_query_);
            return unexpected(message);
        }
        const auto *name = reinterpret_cast<const char *>(sqlite3_column_text(staff_query_, 1));
        const auto skill = sqlite3_column_int(staff_query_, 2);
        if (name == nullptr || skill < static_cast<int>(Skill::SeniorRN) ||
            skill > static_cast<int>(Skill::HCA) || staff_cache_.size() == kMaxStaff) {
            return unexpected(std::string("Database contains invalid staff data"));
        }
        staff_cache_.push_back(
            Staff{StaffId{sqlite3_column_int64(staff_query_, 0)}, name, static_cast<Skill>(skill)});
    }
    sqlite3_reset(staff_query_);
    return staff_cache_;
}

Expected<Day, std::string> SqliteRepo::next_window_start(Day today) {
    sqlite3_reset(latest_run_query_);
    const int status = sqlite3_step(latest_run_query_);
    if (status == SQLITE_DONE) {
        sqlite3_reset(latest_run_query_);
        return today;
    }
    if (status != SQLITE_ROW) {
        const auto message = sqlite_error(database_, "Reading latest run");
        sqlite3_reset(latest_run_query_);
        return unexpected(message);
    }
    auto previous_end = read_date(latest_run_query_, 0);
    sqlite3_reset(latest_run_query_);
    if (!previous_end) {
        return unexpected(previous_end.error());
    }
    return *previous_end + std::chrono::days{1};
}

Expected<void, std::string> SqliteRepo::save_run(const RunData &data) {
    if (staff_cache_.empty()) {
        return unexpected(std::string("Staff must be loaded before saving a run"));
    }
    std::string error;
    if (!execute(database_, "BEGIN IMMEDIATE;", error)) {
        return unexpected(std::move(error));
    }
    bool committed = false;
    const auto rollback = [&] {
        if (!committed) {
            std::string ignored;
            static_cast<void>(execute(database_, "ROLLBACK;", ignored));
        }
    };
    for (const auto &[date, plan] : data.days) {
        for (std::size_t slot = 0; slot < plan.size(); ++slot) {
            auto mask = plan[slot];
            while (mask != 0) {
                const auto index = static_cast<std::size_t>(std::countr_zero(mask));
                if (index >= staff_cache_.size()) {
                    rollback();
                    return unexpected(std::string("Roster references an unknown staff index"));
                }
                const auto date_text =
                    std::format("{:%Y-%m-%d}", std::chrono::year_month_day{date});
                sqlite3_bind_text(assignment_insert_, 1, date_text.c_str(), -1, SQLITE_TRANSIENT);
                sqlite3_bind_int(assignment_insert_, 2, static_cast<int>(slot));
                sqlite3_bind_int64(assignment_insert_, 3, staff_cache_[index].id.value);
                if (sqlite3_step(assignment_insert_) != SQLITE_DONE) {
                    const auto message = sqlite_error(database_, "Saving assignment");
                    sqlite3_reset(assignment_insert_);
                    sqlite3_clear_bindings(assignment_insert_);
                    rollback();
                    return unexpected(message);
                }
                sqlite3_reset(assignment_insert_);
                sqlite3_clear_bindings(assignment_insert_);
                mask &= mask - 1;
            }
        }
    }
    const auto start =
        std::format("{:%Y-%m-%d}", std::chrono::year_month_day{data.run.window_start});
    const auto end = std::format("{:%Y-%m-%d}", std::chrono::year_month_day{data.run.window_end});
    const auto time = std::chrono::floor<std::chrono::seconds>(data.run.ran_at);
    const auto ran_at = std::format("{:%Y-%m-%dT%H:%M:%SZ}", time);
    sqlite3_bind_text(run_insert_, 1, ran_at.c_str(), -1, SQLITE_TRANSIENT);
    sqlite3_bind_text(run_insert_, 2, start.c_str(), -1, SQLITE_TRANSIENT);
    sqlite3_bind_text(run_insert_, 3, end.c_str(), -1, SQLITE_TRANSIENT);
    sqlite3_bind_int(run_insert_, 4, static_cast<int>(data.run.attempts));
    sqlite3_bind_int(run_insert_, 5, data.run.used_fallback ? 1 : 0);
    if (sqlite3_step(run_insert_) != SQLITE_DONE) {
        const auto message = sqlite_error(database_, "Saving roster run");
        sqlite3_reset(run_insert_);
        sqlite3_clear_bindings(run_insert_);
        rollback();
        return unexpected(message);
    }
    sqlite3_reset(run_insert_);
    sqlite3_clear_bindings(run_insert_);
    if (!execute(database_, "COMMIT;", error)) {
        rollback();
        return unexpected(std::move(error));
    }
    committed = true;
    return {};
}
} // namespace roster
