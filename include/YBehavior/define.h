#ifndef _YBEHAVIOR_DEFINE_H_
#define _YBEHAVIOR_DEFINE_H_

#include "types.h"

namespace YBehavior
{
	//#ifdef YBEHAVIOR_DLL
#define YBEHAVIOR_DLL_ENTRY_IMPORT						__declspec(dllimport)
#define YBEHAVIOR_DLL_ENTRY_EXPORT						__declspec(dllexport)
//#else
//#define YBEHAVIOR_DLL_ENTRY_IMPORT
//#define YBEHAVIOR_DLL_ENTRY_EXPORT
//#endif

#ifdef YBEHAVIOR_EXPORTS
#	define YBEHAVIOR_API YBEHAVIOR_DLL_ENTRY_EXPORT
#else
#	define YBEHAVIOR_API YBEHAVIOR_DLL_ENTRY_IMPORT
#endif

}

#endif