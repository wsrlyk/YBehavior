#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"

extern "C" YBEHAVIOR_API void GetSharedVariableToBuffer(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::TYPEID type)
{
	auto data = pAgent->GetMemory()->GetMainData()->Get(key, type);
	YBehavior::SharpBuffer::Set(data, type);
}

extern "C" YBEHAVIOR_API void* GetSharedVariablePtr(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::TYPEID type)
{
	return pAgent->GetMemory()->GetMainData()->Get(key, type);
}

extern "C" YBEHAVIOR_API void SetSharedVariable(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::TYPEID type)
{
	pAgent->GetMemory()->GetMainData()->Set(key, type, YBehavior::SharpBuffer::Get(type));
}

extern "C" YBEHAVIOR_API int GetSharedEntityIndex(YBehavior::Agent* pAgent, YBehavior::KEY key)
{
	YBehavior::EntityWrapper wrapper;
	if (pAgent->GetMemory()->GetMainData()->Get(key, wrapper) && wrapper.IsValid())
	{
		if (auto e = static_cast<YBehavior::SharpEntity*>(wrapper.Get()))
		{
			return e->GetIndex();
		}
	}
	return -1;
}



extern "C" YBEHAVIOR_API YBehavior::KEY GetOrCreateVariableKeyByName(YBehavior::CSTRING name)
{
	return YBehavior::TreeKeyMgr::Instance()->CreateKeyByName(name);
}

#define SHAREDDATA_ALLTYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API YBehavior::TYPEID GetTypeId##TYPE()\
{\
	return YBehavior::GetTypeID<YB::TYPE>();\
}
FOR_EACH_TYPE(SHAREDDATA_ALLTYPES_OPERATIONS);

#endif