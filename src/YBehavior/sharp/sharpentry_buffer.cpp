#ifdef SHARP
#include "YBehavior/sharp/sharpentry_buffer.h"

YBehavior::SharpBuffer YBehavior::SharpBuffer::s_Buffer;

extern "C" YBEHAVIOR_API YBehavior::INT GetFromBufferInt()
{
	return YBehavior::SharpBuffer::s_Buffer.m_Int;
}

extern "C" YBEHAVIOR_API void SetToBufferInt(YBehavior::INT data)
{
	YBehavior::SharpBuffer::s_Buffer.m_Int = data;
}

extern "C" YBEHAVIOR_API YBehavior::FLOAT GetFromBufferFloat()
{
	return YBehavior::SharpBuffer::s_Buffer.m_Float;
}

extern "C" YBEHAVIOR_API void SetToBufferFloat(YBehavior::FLOAT data)
{
	YBehavior::SharpBuffer::s_Buffer.m_Float = data;
}

extern "C" YBEHAVIOR_API YBehavior::ULONG GetFromBufferUlong()
{
	return YBehavior::SharpBuffer::s_Buffer.m_Ulong;
}

extern "C" YBEHAVIOR_API void SetToBufferUlong(YBehavior::ULONG data)
{
	YBehavior::SharpBuffer::s_Buffer.m_Ulong = data;
}

extern "C" YBEHAVIOR_API YBehavior::Vector3 GetFromBufferVector3()
{
	return YBehavior::SharpBuffer::s_Buffer.m_Vector3;
}

extern "C" YBEHAVIOR_API void SetToBufferVector3(YBehavior::Vector3 data)
{
	YBehavior::SharpBuffer::s_Buffer.m_Vector3 = data;
}

extern "C" YBEHAVIOR_API YBehavior::BOOL GetFromBufferBool()
{
	return YBehavior::SharpBuffer::s_Buffer.m_Bool;
}

extern "C" YBEHAVIOR_API void SetToBufferBool(YBehavior::BOOL data)
{
	YBehavior::SharpBuffer::s_Buffer.m_Bool = data;
}

extern "C" YBEHAVIOR_API void GetFromBufferString(char* output, int len)
{
	strcpy_s(output, (size_t)len, YBehavior::SharpBuffer::s_Buffer.m_String.c_str());
}

extern "C" YBEHAVIOR_API void SetToBufferString(char* data)
{
	YBehavior::SharpBuffer::s_Buffer.m_String = data;
}

extern "C" YBEHAVIOR_API YBehavior::Entity* GetFromBufferEntity()
{
	if (YBehavior::SharpBuffer::s_Buffer.m_EntityWrapper.IsValid())
		return 	YBehavior::SharpBuffer::s_Buffer.m_EntityWrapper.Get();
	return nullptr;
}

extern "C" YBEHAVIOR_API void SetToBufferEntity(YBehavior::Entity* data)
{
	if (data)
		YBehavior::SharpBuffer::s_Buffer.m_EntityWrapper = data->GetWrapper();
	else
		YBehavior::SharpBuffer::s_Buffer.m_EntityWrapper.Reset();
}

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////

extern "C" YBEHAVIOR_API void* GetFromBufferVector(YBehavior::TYPEID type)
{
	return YBehavior::SharpBuffer::Get(type);
}

#endif