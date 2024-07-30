#ifdef YSHARP
#ifndef _SHARPUTILITY_H_
#define _SHARPUTILITY_H_

#include "YBehavior/types/types.h"

namespace YBehavior
{
	typedef void (STDCALL *SharpGetFilePathDelegate)();

	class SharpUtility
	{
	public:
		
		static STRING GetFilePath(const STRING& file);
		static void SetGetFilePathCallback(SharpGetFilePathDelegate callback) { s_GetFilePathCallback = callback; }
	private:
		static SharpGetFilePathDelegate s_GetFilePathCallback;
	};
}


#endif // _SHARPUTILITY_H_
#endif