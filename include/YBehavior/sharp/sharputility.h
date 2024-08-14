#ifdef YSHARP
#ifndef _SHARPUTILITY_H_
#define _SHARPUTILITY_H_

#include "YBehavior/types/types.h"

namespace YBehavior
{
	typedef void (STDCALL *SharpGetFilePathDelegate)();
#ifdef YDEBUGGER
	typedef void(STDCALL* OnDebugStateChangedDelegate)(bool isDebugging);
#endif
	class SharpUtility
	{
	public:
		static STRING GetFilePath(const STRING& file);
#ifdef YDEBUGGER
		static void OnDebugStateChanged(bool isDebugging);
#endif

		static void SetGetFilePathCallback(SharpGetFilePathDelegate callback) { s_GetFilePathCallback = callback; }
#ifdef YDEBUGGER
		static void SetOnDebugStateChangedCallback(OnDebugStateChangedDelegate callback) { s_OnDebugStateChangedCallback = callback; }
#endif

	private:
		static SharpGetFilePathDelegate s_GetFilePathCallback;
#ifdef YDEBUGGER
		static OnDebugStateChangedDelegate s_OnDebugStateChangedCallback;
#endif
	};
}


#endif // _SHARPUTILITY_H_
#endif