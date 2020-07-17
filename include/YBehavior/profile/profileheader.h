#ifndef _YBEHAVIOR_PROFILEHEADER_H_
#define _YBEHAVIOR_PROFILEHEADER_H_

#ifdef YPROFILER
#include "YBehavior/profile/profilehelper.h"
#define PROFILER_PAUSE m_pProfileHelper->Pause()
#define PROFILER_RESUME m_pProfileHelper->Resume()
#else
#define PROFILER_PAUSE
#define PROFILER_RESUME
#endif

#endif
