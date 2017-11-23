#ifndef _YBEHAVIOR_LOGGER_H_
#define _YBEHAVIOR_LOGGER_H_

#include "iostream"

namespace YBehavior
{

#define LOG_BEGIN std::cout
#define LOG_END std::endl

#define ERROR_BEGIN std::cout << "ERROR: "
#define ERROR_END std::endl

}

#endif