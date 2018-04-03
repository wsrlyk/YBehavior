PROJECT(YBehavior)  
  
cmake_minimum_required(VERSION 2.8)  
  
add_definitions(-std=c++11)  
  
SET(ENV{Platform} x64)  
SET(ENV{Configuration} Debug)  
 
SET(EXECUTABLE_OUTPUT_PATH  bin/)  
SET(LINK_LIBPATH lib/)  

add_subdirectory(src/YBehavior)  
add_Subdirectory(src/Test)
