#pragma once

#include <chrono>
#include <condition_variable>
#include <functional>
#include <mutex>
#include <stop_token>

namespace roster {

class Scheduler {
  public:
    Scheduler(std::chrono::steady_clock::duration interval, std::function<void()> tick);
    void run(std::stop_token stop);

  private:
    std::chrono::steady_clock::duration interval_;
    std::function<void()> tick_;
    std::mutex mutex_;
    std::condition_variable_any condition_;
};

} // namespace roster
