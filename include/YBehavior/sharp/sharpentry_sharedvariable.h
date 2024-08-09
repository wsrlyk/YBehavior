#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"

extern "C" YBEHAVIOR_API void GetSharedVariable(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	auto data = pAgent->GetMemory()->GetMainData()->Get(key, type);
	///> Let KEY and TYPEID be the same
	YBehavior::SharpBuffer::Set(data, type);
}

extern "C" YBEHAVIOR_API void* GetSharedVariableValuePtr(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	return pAgent->GetMemory()->GetMainData()->Get(key, type);
}

extern "C" YBEHAVIOR_API void SetSharedVariable(YBehavior::Agent* pAgent, YBehavior::KEY key, YBehavior::KEY type)
{
	pAgent->GetMemory()->GetMainData()->Set(key, type, YBehavior::SharpBuffer::Get(type));
}


extern "C" YBEHAVIOR_API YBehavior::KEY GetTypeKeyByName(YBehavior::CSTRING name)
{
	return YBehavior::TreeKeyMgr::Instance()->GetKeyByName(name);
}

#define SHAREDDATA_ALLTYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API YBehavior::TYPEID GetClassTypeNumberId##TYPE()\
{\
	return YBehavior::GetTypeID<YB::TYPE>();\
}
FOR_EACH_TYPE(SHAREDDATA_ALLTYPES_OPERATIONS);

#endif