#pragma once
#include "YBehavior/types.h"
#include "Ybehavior/agent.h"
#include "YBehavior/interface.h"

extern "C" YBEHAVIOR_API const void* GetVariableValue(YBehavior::Agent* pAgent, YBehavior::ISharedVariableEx* pVariable)
{
	if (pVariable != nullptr && pAgent != nullptr)
		return pVariable->GetValue(pAgent->GetSharedData());
	return nullptr;
}

extern "C" YBEHAVIOR_API void SetVariableValue(YBehavior::Agent* pAgent, YBehavior::ISharedVariableEx* pVariable, void* value)
{
	if (pVariable != nullptr && pAgent != nullptr)
		pVariable->SetValue(pAgent->GetSharedData(), value);
}

template<typename T>
T GetVariableValue(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<T>* pVariable)
{
	if (pVariable != nullptr && pAgent != nullptr)
	{
		T res = YBehavior::GetTypeDefaultValue<T>();
		pVariable->GetCastedValue(pAgent->GetSharedData(), res);
		return res;
	}
	return YBehavior::GetTypeDefaultValue<T>();
}

template<typename T>
void SetVariableValue(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<T>* pVariable, const T& value)
{
	if (pVariable != nullptr && pAgent != nullptr)
	{
		pVariable->SetCastedValue(pAgent->GetSharedData(), &value);
	}
}

extern "C" YBEHAVIOR_API YBehavior::TYPEID GetVariableTypeID(YBehavior::ISharedVariableEx* pVariable)
{
	return pVariable->GetTypeID();
}

#define VARIABLE_SIMPLETYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API YBehavior::##TYPE GetVariable##TYPE(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::##TYPE>* pVariable)\
{\
	return GetVariableValue<YBehavior::##TYPE>(pAgent, pVariable);\
}\
extern "C" YBEHAVIOR_API void SetVariable##TYPE(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::##TYPE>* pVariable, YBehavior::##TYPE value)\
{\
	SetVariableValue<YBehavior::##TYPE>(pAgent, pVariable, value);\
}

VARIABLE_SIMPLETYPES_OPERATIONS(Int);
VARIABLE_SIMPLETYPES_OPERATIONS(Ulong);
VARIABLE_SIMPLETYPES_OPERATIONS(Float);
VARIABLE_SIMPLETYPES_OPERATIONS(Bool);
VARIABLE_SIMPLETYPES_OPERATIONS(Vector3);

extern "C" YBEHAVIOR_API YBehavior::CSTRING GetVariableString(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::STRING>* pVariable)
{
	if (pVariable != nullptr && pAgent != nullptr)
	{
		const YBehavior::STRING* res = pVariable->GetCastedValue(pAgent->GetSharedData());
		return res ? res->c_str() : nullptr;
	}
	return nullptr;
}
extern "C" YBEHAVIOR_API void SetVariableString(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::STRING>* pVariable, YBehavior::CSTRING value)
{
	YBehavior::STRING s(value);
	SetVariableValue<YBehavior::STRING>(pAgent, pVariable, s);
}

extern "C" YBEHAVIOR_API YBehavior::Entity* GetEntityFromWrapper(YBehavior::EntityWrapper* pWrapper)
{
	if (pWrapper) return pWrapper->Get();
	else return nullptr;
}

extern "C" YBEHAVIOR_API YBehavior::Entity* GetEntityFromVariable(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::EntityWrapper>* pVariable)
{
	if (pVariable != nullptr && pAgent != nullptr)
	{
		const YBehavior::EntityWrapper* res = pVariable->GetCastedValue(pAgent->GetSharedData());
		if (res)
			return res->Get();
		return nullptr;
	}
	return nullptr;
}

extern "C" YBEHAVIOR_API void SetEntityToVariable(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::EntityWrapper>* pVariable, YBehavior::Entity* pEntity)
{
	if (pVariable != nullptr && pAgent != nullptr && pEntity != nullptr)
	{
		YBehavior::EntityWrapper wrapper(pEntity->CreateWrapper());
		pVariable->SetCastedValue(pAgent->GetSharedData(), &wrapper);
	}
}
