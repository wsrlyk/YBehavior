set(ProjectName Test)

INCLUDE_DIRECTORIES(../../include)

AUX_SOURCE_DIRECTORY(. DIR_SRCS)
list(REMOVE_ITEM DIR_SRCS ./main.cpp ./main-benchmark.cpp ./main-reload.cpp)

LINK_DIRECTORIES(${CMAKE_CURRENT_SOURCE_DIR}/lib/)
SET(EXECUTABLE_OUTPUT_PATH  ../../../bin/)

ADD_EXECUTABLE(${ProjectName} ${DIR_SRCS})

TARGET_LINK_LIBRARIES ( ${ProjectName} YBehavior pthread)
