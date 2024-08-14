#ifdef YSHARP

#include "YBehavior/sharp/sharputility.h"
#include "YBehavior/sharp/sharpentry_buffer.h"

namespace YBehavior
{
	STRING SharpUtility::GetFilePath(const STRING& file)
	{
		if (s_GetFilePathCallback)
		{
			SharpBuffer::s_Buffer.m_String = file;
			s_GetFilePathCallback();
			return SharpBuffer::s_Buffer.m_String;
		}
		return file;
	}

#ifdef YDEBUGGER
	void SharpUtility::OnDebugStateChanged(bool isDebugging)
	{
		if (s_OnDebugStateChangedCallback)
		{
			s_OnDebugStateChangedCallback(isDebugging);
		}
	}
#endif

	YBehavior::SharpGetFilePathDelegate SharpUtility::s_GetFilePathCallback;

#ifdef YDEBUGGER
	YBehavior::OnDebugStateChangedDelegate SharpUtility::s_OnDebugStateChangedCallback;
#endif

}

#endif