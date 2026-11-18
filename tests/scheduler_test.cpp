#include "app/scheduler.hpp"

#include <gtest/gtest.h>

#include <atomic>
#include <chrono>
#include <thread>

namespace roster {
TEST(Scheduler, DoesNotOverlapTicksAndStopsCleanly) {
    std::atomic<int> active{0};
    std::atomic<int> maximum_active{0};
    std::atomic<int> calls{0};
    Scheduler scheduler(std::chrono::milliseconds{5}, [&] {
        const int current = ++active;
        int observed = maximum_active.load();
        while (observed < current && !maximum_active.compare_exchange_weak(observed, current)) {
        }
        ++calls;
        std::this_thread::sleep_for(std::chrono::milliseconds{20});
        --active;
    });
    std::jthread worker([&](std::stop_token token) { scheduler.run(token); });
    while (calls.load() < 3) {
        std::this_thread::sleep_for(std::chrono::milliseconds{1});
    }
    worker.request_stop();
    worker.join();
    EXPECT_EQ(maximum_active.load(), 1);
    EXPECT_EQ(active.load(), 0);
}
} // namespace roster
