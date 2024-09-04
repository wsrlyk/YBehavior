#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"
#include "sharpentry_buffer.h"
#include "YBehavior/eventqueue.h"

extern "C" YBEHAVIOR_API YBehavior::EventQueue::Event* CreateEvent(YBehavior::Agent* pAgent, YBehavior::CSTRING_CONST name)
{
	if (auto pQueue = pAgent->GetEventQueue())
	{
		return pQueue->Create(name);
	}
	return nullptr;
}
extern "C" YBEHAVIOR_API void RegisterEvent(YBehavior::Agent* pAgent, YBehavior::CSTRING_CONST name)
{
	if (auto pQueue = pAgent->GetEventQueue())
	{
		pQueue->RegisterEvent(name, 1);
	}
}

extern "C" YBEHAVIOR_API void AssignEventParam(YBehavior::Agent* pAgent, YBehavior::EventQueue::Event* pEvent, void* pVector, YBehavior::TYPEID elementTypeID)
{
	switch (elementTypeID)
	{
	case YBehavior::GetTypeID<YBehavior::INT>():
		pEvent->Assign(*static_cast<YBehavior::VecInt*>(pVector));
		break;
	case YBehavior::GetTypeID<YBehavior::FLOAT>():
		pEvent->Assign(*static_cast<YBehavior::VecFloat*>(pVector));
		break;
	case YBehavior::GetTypeID<YBehavior::ULONG>():
		pEvent->Assign(*static_cast<YBehavior::VecUlong*>(pVector));
		break;
	case YBehavior::GetTypeID<YBehavior::BOOL>():
		pEvent->Assign(*static_cast<YBehavior::VecBool*>(pVector));
		break;
	case YBehavior::GetTypeID<YBehavior::Vector3>():
		pEvent->Assign(*static_cast<YBehavior::VecVector3*>(pVector));
		break;
	case YBehavior::GetTypeID<YBehavior::STRING>():
		pEvent->Assign(*static_cast<YBehavior::VecString*>(pVector));
		break;
	case YBehavior::GetTypeID<YBehavior::EntityWrapper>():
		pEvent->Assign(*static_cast<YBehavior::VecEntityWrapper*>(pVector));
		break;
	}
}

#endif