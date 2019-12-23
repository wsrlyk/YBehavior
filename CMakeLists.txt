PROJECT(YBehavior)  
  
cmake_minimum_required(VERSION 3.3.0)  
  
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -std=c++17")
SET(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS} -O0 -g -DDEBUGGER -Werror -Wno-unused-function -Wno-invalid-offsetof -Wno-maybe-uninitialized")
SET(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS} -O1 -g -Werror -Wno-unused-function -Wno-invalid-offsetof -Wno-maybe-uninitialized")

SET(EXECUTABLE_OUTPUT_PATH  bin/)  
SET(LINK_LIBPATH ${CMAKE_CURRENT_SOURCE_DIR}/lib/)  

add_subdirectory(src/YBehavior)  
add_Subdirectory(src/Test)
