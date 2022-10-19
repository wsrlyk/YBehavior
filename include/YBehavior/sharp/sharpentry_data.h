#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/interface.h"
#include "YBehavior/utility.h"

extern "C" YBEHAVIOR_API YBehavior::INT ToInt(const void* ptr)
{
	if (ptr)
		return *((const int*)ptr);
	return 0;
}

extern "C" YBEHAVIOR_API YBehavior::ULONG ToUlong(const void* ptr)
{
	if (ptr)
		return *((const YBehavior::ULONG*)ptr);
	return 0;
}

extern "C" YBEHAVIOR_API YBehavior::FLOAT ToFloat(const void* ptr)
{
	if (ptr)
		return *((const YBehavior::FLOAT*)ptr);
	return 0;
}

extern "C" YBEHAVIOR_API YBehavior::BOOL ToBool(const void* ptr)
{
	if (ptr)
		return *((const YBehavior::BOOL*)ptr);
	return YBehavior::Utility::FALSE_VALUE;
}

extern "C" YBEHAVIOR_API YBehavior::Vector3 ToVector3(const void* ptr)
{
	if (ptr)
		return *((const YBehavior::Vector3*)ptr);
	return YBehavior::Vector3::zero;
}

extern "C" YBEHAVIOR_API bool ToString(const void* ptr, char* output, int len)
{
	if (ptr && output)
	{
		snprintf(output, (size_t)len, "%s", ((const YBehavior::STRING*)ptr)->c_str());
		//strncpy(output, ((const YBehavior::STRING*)ptr)->c_str(), (unsigned)len);
		return true;
	}
	return false;
}

extern "C" YBEHAVIOR_API YBehavior::Entity* ToEntity(const void* ptr)
{
	if (ptr)
	{
		if (((const YBehavior::EntityWrapper*)ptr)->IsValid())
			return ((const YBehavior::EntityWrapper*)ptr)->Get();
	}
	return nullptr;
}
#endif