#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"
#include "sharpentry_buffer.h"

extern "C" YBEHAVIOR_API bool GetPinValue(YBehavior::Agent* pAgent, YBehavior::IPin* pPin)
{
	if (pPin != nullptr && pAgent != nullptr)
	{
		auto data = pPin->GetValuePtr(pAgent->GetMemory());
		YBehavior::SharpBuffer::Set(data, pPin->TypeID());
		return true;
	}
	return false;
}

extern "C" YBEHAVIOR_API const void* GetPinValuePtr(YBehavior::Agent* pAgent, YBehavior::IPin* pPin)
{
	if (pPin != nullptr && pAgent != nullptr)
	{
		return pPin->GetValuePtr(pAgent->GetMemory());
	}
	return nullptr;
}

extern "C" YBEHAVIOR_API void SetPinValue(YBehavior::Agent* pAgent, YBehavior::IPin* pPin)
{
	if (pPin != nullptr && !pPin->IsConst() && pAgent != nullptr)
		pPin->SetValue(pAgent->GetMemory(), YBehavior::SharpBuffer::Get(pPin->TypeID()));
}

extern "C" YBEHAVIOR_API YBehavior::TYPEID GetPinTypeID(YBehavior::IPin* pPin)
{
	return pPin->TypeID();
}

extern "C" YBEHAVIOR_API YBehavior::TYPEID GetPinElementTypeID(YBehavior::IPin* pPin)
{
	return pPin->ElementTypeID();
}
#endif