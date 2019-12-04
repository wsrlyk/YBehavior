PROJECT(YBehavior)  
  
cmake_minimum_required(VERSION 2.8)  
  
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -std=c++17")  

SET(ENV{Platform} x64)  
SET(ENV{Configuration} Debug)  
 
SET(EXECUTABLE_OUTPUT_PATH  bin/)  
SET(LINK_LIBPATH ${CMAKE_CURRENT_SOURCE_DIR}/lib/)  

add_subdirectory(src/YBehavior)  
add_Subdirectory(src/Test)
