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

extern "C" YBEHAVIOR_API int GetPinEntityIndex(YBehavior::Agent* pAgent, YBehavior::IPin* pPin)
{
	if (pPin != nullptr && pPin->TypeID() == YBehavior::GetTypeID<YBehavior::EntityWrapper>() && pAgent != nullptr)
	{
		YBehavior::EntityWrapper wrapper;
		static_cast<YBehavior::Pin<YBehavior::EntityWrapper>*>(pPin)->GetValue(pAgent->GetMemory(), wrapper);
		if (wrapper.IsValid())
		{
			if (auto e = static_cast<YBehavior::SharpEntity*>(wrapper.Get()))
			{
				return e->GetIndex();
			}
		}
	}
	return -1;
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

extern "C" YBEHAVIOR_API bool IsPinConst(YBehavior::IPin* pPin)
{
	return pPin && pPin->IsConst();
}
#endif