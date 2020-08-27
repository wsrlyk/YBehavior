#pragma once
#include "YBehavior/types.h"
#include "Ybehavior/agent.h"
#include "YBehavior/interface.h"

extern "C" YBEHAVIOR_API void GetSharedData(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	auto data = pAgent->GetMemory()->GetMainData()->Get(key, type);
	///> Let KEY and TYPEID be the same
	YBehavior::SharpBuffer::Set(data, type);
}

extern "C" YBEHAVIOR_API void* GetSharedDataValuePtr(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	return pAgent->GetMemory()->GetMainData()->Get(key, type);
}

extern "C" YBEHAVIOR_API void SetSharedData(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	pAgent->GetMemory()->GetMainData()->Set(key, type, YBehavior::SharpBuffer::Get(type));
}


extern "C" YBEHAVIOR_API YBehavior::KEY GetTypeKeyByName(YBehavior::CSTRING name, YBehavior::TYPEID type)
{
	return YBehavior::TreeKeyMgr::Instance()->GetKeyByName(name, type);
}

#define SHAREDDATA_ALLTYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API YBehavior::TYPEID GetClassTypeNumberId##TYPE()\
{\
	return YBehavior::GetTypeID<YBehavior::##TYPE>();\
}
FOR_EACH_TYPE(SHAREDDATA_ALLTYPES_OPERATIONS);

//extern "C" YBEHAVIOR_API YBehavior::CSTRING_CONST GetSharedDataString(YBehavior::Agent* pAgent, YBehavior::KEY key)
//{
//	const YBehavior::STRING* str = pAgent->GetMemory()->GetMainData()->Get<YBehavior::STRING>(key);
//	if (str)
//		return str->c_str();
//	return YBehavior::Utility::StringEmpty.c_str();
//}
//extern "C" YBEHAVIOR_API bool GetSharedDataString(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::CSTRING outputStr, unsigned maxLength)
//{
//	const YBehavior::STRING* str = pAgent->GetMemory()->GetMainData()->Get<YBehavior::STRING>(key);
//	if (str)
//	{
//		strcpy_s(outputStr, maxLength, str->c_str());
//		return true;
//	}
//	return false;
//}
//extern "C" YBEHAVIOR_API void SetSharedDataString(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::CSTRING_CONST value)
//{
//	pAgent->GetMemory()->GetMainData()->Set<YBehavior::STRING>(key, value);
//}
//
//extern "C" YBEHAVIOR_API YBehavior::Entity* GetSharedDataEntity(YBehavior::Agent* pAgent, YBehavior::KEY key)
//{
//	if (pAgent != nullptr)
//	{
//		const YBehavior::EntityWrapper* res = pAgent->GetMemory()->GetMainData()->Get<YBehavior::EntityWrapper>(key);
//		if (res && res->IsValid())
//			return res->Get();
//		return nullptr;
//	}
//	return nullptr;
//}
//
//extern "C" YBEHAVIOR_API void SetEntityToSharedData(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::Entity* pEntity)
//{
//	if (pAgent != nullptr && pEntity != nullptr)
//	{
//		YBehavior::EntityWrapper wrapper(pEntity->GetWrapper());
//		pAgent->GetMemory()->GetMainData()->Set<YBehavior::EntityWrapper>(key, &wrapper);
//	}
//}
