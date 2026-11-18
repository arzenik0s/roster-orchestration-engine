#include "adapters/ollama_client.hpp"

#include "adapters/response_parser.hpp"

#include <cpr/cpr.h>
#include <nlohmann/json.hpp>

#include <algorithm>
#include <memory>

namespace roster {
class OllamaClient::Session {
  public:
    cpr::Session connection;
};

OllamaClient::OllamaClient(OllamaOptions options)
    : options_(std::move(options)), session_(std::make_unique<Session>()) {
    while (!options_.url.empty() && options_.url.back() == '/') {
        options_.url.pop_back();
    }
}

OllamaClient::~OllamaClient() = default;

Expected<DayPlan, LlmError> OllamaClient::propose(std::string_view prompt,
                                                  const std::vector<Staff> &staff) {
    nlohmann::json ids = nlohmann::json::array();
    for (const auto &member : staff) {
        ids.push_back(member.id.value);
    }
    const nlohmann::json format = {{"type", "object"},
                                   {"properties",
                                    {{"AM",
                                      {{"type", "array"},
                                       {"items", {{"type", "integer"}, {"enum", ids}}},
                                       {"minItems", 5},
                                       {"maxItems", 5}}},
                                     {"PM",
                                      {{"type", "array"},
                                       {"items", {{"type", "integer"}, {"enum", ids}}},
                                       {"minItems", 4},
                                       {"maxItems", 4}}},
                                     {"Night",
                                      {{"type", "array"},
                                       {"items", {{"type", "integer"}, {"enum", ids}}},
                                       {"minItems", 3},
                                       {"maxItems", 3}}}}},
                                   {"required", {"AM", "PM", "Night"}},
                                   {"additionalProperties", false}};
    const nlohmann::json body = {
        {"model", options_.model},
        {"messages", nlohmann::json::array({{{"role", "user"}, {"content", prompt}}})},
        {"stream", false},
        {"format", format},
        {"options", {{"temperature", options_.temperature}, {"seed", options_.seed}}},
        {"keep_alive", "10m"}};

    auto &connection = session_->connection;
    connection.SetUrl(cpr::Url{options_.url + "/api/chat"});
    connection.SetHeader(cpr::Header{{"Content-Type", "application/json"}});
    connection.SetConnectTimeout(cpr::ConnectTimeout{options_.connect_timeout});
    connection.SetTimeout(cpr::Timeout{options_.read_timeout});
    connection.SetBody(cpr::Body{body.dump()});
    const auto response = connection.Post();
    if (response.error) {
        return unexpected(LlmError{"Ollama request failed: " + response.error.message});
    }
    if (response.status_code < 200 || response.status_code >= 300) {
        return unexpected(LlmError{"Ollama returned HTTP " + std::to_string(response.status_code)});
    }
    try {
        const auto result = nlohmann::json::parse(response.text);
        if (!result.is_object() || !result.contains("message") || !result["message"].is_object() ||
            !result["message"].contains("content") || !result["message"]["content"].is_string()) {
            return unexpected(LlmError{"Ollama response has an invalid message shape"});
        }
        auto parsed = parse_response(result["message"]["content"].get<std::string>(), staff);
        if (!parsed) {
            return unexpected(LlmError{parsed.error()});
        }
        return parsed.value();
    } catch (const nlohmann::json::exception &error) {
        return unexpected(LlmError{std::string("Invalid Ollama JSON: ") + error.what()});
    }
}
} // namespace roster
