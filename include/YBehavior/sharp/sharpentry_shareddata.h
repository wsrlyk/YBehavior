#pragma once
#include "YBehavior/types.h"
#include "Ybehavior/agent.h"
#include "YBehavior/interface.h"

extern "C" YBEHAVIOR_API const void* GetSharedDataValue(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	return pAgent->GetMemory()->GetMainData()->Get(key, type);
}
extern "C" YBEHAVIOR_API void SetSharedDataValue(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type, const void* value)
{
	pAgent->GetMemory()->GetMainData()->Set(key, type, value);
}

#define SHAREDDATA_NORMALTYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API YBehavior::KEY Get##TYPE##KeyByName(YBehavior::CSTRING name)\
{\
	YBehavior::STRING str(name);\
	return YBehavior::TreeKeyMgr::Instance()->GetKeyByName<YBehavior::##TYPE>(str);\
}
FOR_EACH_SINGLE_NORMAL_TYPE(SHAREDDATA_NORMALTYPES_OPERATIONS);

#define SHAREDDATA_ALLTYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API YBehavior::TYPEID GetClassTypeNumberId##TYPE()\
{\
	return YBehavior::GetClassTypeNumberId<YBehavior::##TYPE>();\
}


extern "C" YBEHAVIOR_API YBehavior::CSTRING GetSharedDataString(YBehavior::Agent* pAgent, YBehavior::KEY key)
{
	const YBehavior::STRING* str = pAgent->GetMemory()->GetMainData()->Get<YBehavior::STRING>(key);
	if (str)
		return str->c_str();
	return YBehavior::Utility::StringEmpty.c_str();
}
extern "C" YBEHAVIOR_API void SetSharedDataString(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::CSTRING value)
{
	pAgent->GetMemory()->GetMainData()->Set<YBehavior::STRING>(key, value);
}

extern "C" YBEHAVIOR_API YBehavior::Entity* GetSharedDataEntity(YBehavior::Agent* pAgent, YBehavior::KEY key)
{
	if (pAgent != nullptr)
	{
		const YBehavior::EntityWrapper* res = pAgent->GetMemory()->GetMainData()->Get<YBehavior::EntityWrapper>(key);
		if (res && res->IsValid())
			return res->Get();
		return nullptr;
	}
	return nullptr;
}

extern "C" YBEHAVIOR_API void SetEntityToSharedData(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::Entity* pEntity)
{
	if (pAgent != nullptr && pEntity != nullptr)
	{
		YBehavior::EntityWrapper wrapper(pEntity->GetWrapper());
		pAgent->GetMemory()->GetMainData()->Set<YBehavior::EntityWrapper>(key, &wrapper);
	}
}
