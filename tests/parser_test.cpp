#include "adapters/response_parser.hpp"

#include <gtest/gtest.h>

#include <bit>

namespace roster {
namespace {
const std::vector<Staff> kStaff{
    {StaffId{1}, "Senior", Skill::SeniorRN}, {StaffId{2}, "RN", Skill::RN},
    {StaffId{3}, "EN", Skill::EN},           {StaffId{4}, "HCA", Skill::HCA},
    {StaffId{5}, "HCA", Skill::HCA},         {StaffId{6}, "HCA", Skill::HCA},
    {StaffId{7}, "HCA", Skill::HCA},         {StaffId{8}, "HCA", Skill::HCA},
    {StaffId{9}, "HCA", Skill::HCA},         {StaffId{10}, "HCA", Skill::HCA},
    {StaffId{11}, "HCA", Skill::HCA},        {StaffId{12}, "HCA", Skill::HCA}};
}

TEST(ResponseParser, AcceptsOnlyAnExactKnownIdObject) {
    auto parsed = parse_response(R"({"AM":[1,2,3,4,5],"PM":[6,7,8,9],"Night":[10,11,12]})", kStaff);
    ASSERT_TRUE(parsed);
    EXPECT_EQ(std::popcount(parsed->at(0)), 5);
    EXPECT_FALSE(parse_response(R"({"AM":[1,2,3,4,5],"PM":[6,7,8,9],"Night":[10,11,99]})", kStaff));
    EXPECT_FALSE(parse_response(
        R"({"AM":[1,2,3,4,5],"PM":[6,7,8,9],"Night":[10,11,12],"extra":[]})", kStaff));
    EXPECT_FALSE(parse_response(
        "{\"AM\":[1,2,3,4,5],\"PM\":[6,7,8,9],\"Night\":[10,11,12]} trailing", kStaff));
}
} // namespace roster
