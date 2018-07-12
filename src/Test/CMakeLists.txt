set(ProjectName Test)
PROJECT(${ProjectName})

INCLUDE_DIRECTORIES(../../include)

AUX_SOURCE_DIRECTORY(. DIR_SRCS)
list(REMOVE_ITEM DIR_SRCS ./main-reload.cpp)

SET(CMAKE_CXX_COMPILER ${CompilerSetting})

IF ($ENV{Configuration} STREQUAL Debug)
    SET(CMAKE_BUILD_TYPE Debug)
ELSE() 
    SET(CMAKE_BUILD_TYPE Release)
ENDIF()

LINK_DIRECTORIES(../lib/)
SET(EXECUTABLE_OUTPUT_PATH  ../../../bin/)

ADD_DEFINITIONS(-DDEBUGGER)  
ADD_EXECUTABLE(${ProjectName} ${DIR_SRCS})

TARGET_LINK_LIBRARIES ( ${ProjectName} pthread)
TARGET_LINK_LIBRARIES ( ${ProjectName} YBehavior)

