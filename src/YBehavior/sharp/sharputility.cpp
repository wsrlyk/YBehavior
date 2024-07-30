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

	YBehavior::SharpGetFilePathDelegate SharpUtility::s_GetFilePathCallback;

}

#endif