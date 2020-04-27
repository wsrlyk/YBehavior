#ifndef _YBEHAVIOR_PROFILEHEADER_H_
#define _YBEHAVIOR_PROFILEHEADER_H_

#ifdef YPROFILER
#include "YBehavior/profile/profilehelper.h"
#define PROFILER_PAUSE m_pProfileHelper->Pause()
#define PROFILER_RESUME m_pProfileHelper->Resume()
#define PROFILER_ENABLE_TOTAL m_pProfileHelper->CalcTotal()
#else
#define PROFILER_PAUSE
#define PROFILER_RESUME
#define PROFILER_ENABLE_TOTAL
#endif

#endif
