#pragma once

#include "ports/llm_client.hpp"

#include <chrono>
#include <string>

namespace roster {

struct OllamaOptions {
    std::string url;
    std::string model;
    std::chrono::milliseconds connect_timeout{5000};
    std::chrono::milliseconds read_timeout{300000};
    double temperature{0.55};
    std::uint32_t seed{42};
};

class OllamaClient final : public ILlmClient {
  public:
    explicit OllamaClient(OllamaOptions options);
    ~OllamaClient() override;
    [[nodiscard]] Expected<DayPlan, LlmError> propose(std::string_view prompt,
                                                      const std::vector<Staff> &staff) override;

  private:
    OllamaOptions options_;
    class Session;
    std::unique_ptr<Session> session_;
};

} // namespace roster
