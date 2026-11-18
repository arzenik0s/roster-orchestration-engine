#include "app/scheduler.hpp"

#include <stdexcept>
#include <utility>

namespace roster {
Scheduler::Scheduler(std::chrono::steady_clock::duration interval, std::function<void()> tick)
    : interval_(interval), tick_(std::move(tick)) {
    if (interval_ <= std::chrono::steady_clock::duration::zero() || !tick_) {
        throw std::invalid_argument("Scheduler requires a positive interval and a tick callback");
    }
}

void Scheduler::run(std::stop_token stop) {
    std::unique_lock lock(mutex_);
    while (!stop.stop_requested()) {
        lock.unlock();
        tick_();
        lock.lock();
        condition_.wait_for(lock, stop, interval_, [] { return false; });
    }
}
} // namespace roster
