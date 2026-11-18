#include "core/greedy.hpp"
#include "core/validator.hpp"

#include <benchmark/benchmark.h>

#include <vector>

namespace {
std::vector<roster::Staff> staff() {
    std::vector<roster::Staff> people;
    for (std::int64_t i = 0; i < 18; ++i) {
        const auto skill =
            i < 3 ? roster::Skill::SeniorRN
                  : (i < 9 ? roster::Skill::RN : (i < 14 ? roster::Skill::EN : roster::Skill::HCA));
        people.push_back({roster::StaffId{i + 1}, "staff", skill});
    }
    return people;
}

void BM_Validate(benchmark::State &state) {
    const auto people = staff();
    auto plan = roster::greedy(0, people).value();
    const auto rn = roster::rn_mask(people);
    const auto senior = roster::skill_mask(people, roster::Skill::SeniorRN);
    for (auto _ : state) {
        benchmark::DoNotOptimize(roster::validate(plan, rn, senior));
    }
}
BENCHMARK(BM_Validate);

void BM_Greedy(benchmark::State &state) {
    const auto people = staff();
    std::uint64_t rotation = 0;
    for (auto _ : state) {
        benchmark::DoNotOptimize(roster::greedy(rotation++, people));
    }
}
BENCHMARK(BM_Greedy);
} // namespace

BENCHMARK_MAIN();
