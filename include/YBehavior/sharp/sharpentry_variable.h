#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"
#include "sharpentry_buffer.h"

extern "C" YBEHAVIOR_API bool GetVariableValue(YBehavior::Agent* pAgent, YBehavior::ISharedVariableEx* pVariable)
{
	if (pVariable != nullptr && pAgent != nullptr)
	{
		auto data = pVariable->GetValue(pAgent->GetMemory());
		YBehavior::SharpBuffer::Set(data, pVariable->TypeID());
		return true;
	}
	return false;
}

extern "C" YBEHAVIOR_API const void* GetVariableValuePtr(YBehavior::Agent* pAgent, YBehavior::ISharedVariableEx* pVariable)
{
	if (pVariable != nullptr && pAgent != nullptr)
	{
		return pVariable->GetValue(pAgent->GetMemory());
	}
	return nullptr;
}

extern "C" YBEHAVIOR_API void SetVariableValue(YBehavior::Agent* pAgent, YBehavior::ISharedVariableEx* pVariable)
{
	if (pVariable != nullptr && !pVariable->IsConst() && pAgent != nullptr)
		pVariable->SetValue(pAgent->GetMemory(), YBehavior::SharpBuffer::Get(pVariable->TypeID()));
}

extern "C" YBEHAVIOR_API YBehavior::TYPEID GetVariableTypeID(YBehavior::ISharedVariableEx* pVariable)
{
	return pVariable->TypeID();
}

extern "C" YBEHAVIOR_API YBehavior::TYPEID GetVariableElementTypeID(YBehavior::ISharedVariableEx* pVariable)
{
	return pVariable->ElementTypeID();
}
#endif