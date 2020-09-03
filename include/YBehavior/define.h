#ifndef _YBEHAVIOR_DEFINE_H_
#define _YBEHAVIOR_DEFINE_H_

namespace YBehavior
{
#ifdef YBEHAVIOR_DLL
	#define YBEHAVIOR_API						__declspec(dllexport)
#else
	#define YBEHAVIOR_API
#endif

#ifdef _MSC_VER
	#define MSVC
	#define STDCALL _stdcall
#else
	#define GCC
	#define STDCALL
#endif
//#else
//#define YBEHAVIOR_DLL_ENTRY_IMPORT
//#define YBEHAVIOR_DLL_ENTRY_EXPORT
//#endif
#define TOSTRING(s) #s
}

#endif