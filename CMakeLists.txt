cmake_minimum_required(VERSION 3.25)
project(roster_lab LANGUAGES CXX)

option(ROSTER_ENABLE_FUZZING "Build the libFuzzer response-parser target" OFF)
option(ROSTER_ENABLE_TSAN "Build with ThreadSanitizer" OFF)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(ROSTER_ENABLE_TSAN AND NOT MSVC)
  add_compile_options(-fsanitize=thread -fno-omit-frame-pointer)
  add_link_options(-fsanitize=thread)
endif()

if(MSVC)
  add_compile_options(/W4 /WX /permissive-)
else()
  add_compile_options(-Wall -Wextra -Wpedantic -Werror)
endif()

find_package(CLI11 CONFIG REQUIRED)
find_package(cpr CONFIG REQUIRED)
find_package(nlohmann_json CONFIG REQUIRED)
find_package(spdlog CONFIG REQUIRED)
find_package(SQLite3 REQUIRED)
find_package(tomlplusplus CONFIG REQUIRED)
find_package(tl-expected CONFIG REQUIRED)

add_library(roster_core
  src/core/agent.cpp
  src/core/greedy.cpp
  src/core/validator.cpp
)
target_include_directories(roster_core PUBLIC src)
target_compile_features(roster_core PUBLIC cxx_std_20)
target_link_libraries(roster_core PUBLIC tl::expected)

add_library(roster_adapters
  src/adapters/ollama_client.cpp
  src/adapters/response_parser.cpp
  src/adapters/sqlite_repo.cpp
)
target_include_directories(roster_adapters PUBLIC src)
target_link_libraries(roster_adapters
  PUBLIC roster_core cpr::cpr nlohmann_json::nlohmann_json
         spdlog::spdlog SQLite3::SQLite3
)

add_executable(roster-agent
  src/app/config.cpp
  src/app/main.cpp
  src/app/scheduler.cpp
)
target_link_libraries(roster-agent
  PRIVATE roster_adapters CLI11::CLI11 tomlplusplus::tomlplusplus
)

include(CTest)
if(BUILD_TESTING)
  find_package(GTest CONFIG REQUIRED)
  find_package(rapidcheck CONFIG REQUIRED)
  find_package(benchmark CONFIG REQUIRED)

  add_executable(roster-tests
    src/app/config.cpp
    src/app/scheduler.cpp
    tests/agent_test.cpp
    tests/config_test.cpp
    tests/parser_test.cpp
    tests/repo_test.cpp
    tests/scheduler_test.cpp
    tests/validator_test.cpp
    tests/window_test.cpp
  )
  target_link_libraries(roster-tests
    PRIVATE roster_adapters GTest::gtest_main rapidcheck rapidcheck_gtest
            tomlplusplus::tomlplusplus
  )
  add_test(NAME roster-tests COMMAND roster-tests)
  add_test(NAME Scheduler COMMAND roster-tests --gtest_filter=Scheduler.*)

  add_executable(roster-bench bench/roster_bench.cpp)
  target_link_libraries(roster-bench PRIVATE roster_core benchmark::benchmark)
endif()

if(ROSTER_ENABLE_FUZZING)
  if(NOT CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    message(FATAL_ERROR "ROSTER_ENABLE_FUZZING requires Clang/libFuzzer")
  endif()
  add_executable(roster-parser-fuzz fuzz/response_parser_fuzz.cpp
                 src/adapters/response_parser.cpp)
  target_include_directories(roster-parser-fuzz PRIVATE src)
  target_link_libraries(roster-parser-fuzz PRIVATE nlohmann_json::nlohmann_json
                                                    tl::expected)
  target_compile_options(roster-parser-fuzz PRIVATE -fsanitize=fuzzer,address,undefined)
  target_link_options(roster-parser-fuzz PRIVATE -fsanitize=fuzzer,address,undefined)
endif()

