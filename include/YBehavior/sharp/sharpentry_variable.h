#pragma once
#include "YBehavior/types.h"
#include "Ybehavior/agent.h"
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
	if (pVariable != nullptr && pAgent != nullptr)
		pVariable->SetValue(pAgent->GetMemory(), YBehavior::SharpBuffer::Get(pVariable->TypeID()));
}

//template<typename T>
//T GetVariableValue(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<T>* pVariable)
//{
//	if (pVariable != nullptr && pAgent != nullptr)
//	{
//		T res = YBehavior::Utility::Default<T>();
//		pVariable->GetCastedValue(pAgent->GetMemory(), res);
//		return res;
//	}
//	return YBehavior::Utility::Default<T>();
//}
//
//template<typename T>
//void SetVariableValue(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<T>* pVariable, const T& value)
//{
//	if (pVariable != nullptr && pAgent != nullptr)
//	{
//		pVariable->SetCastedValue(pAgent->GetMemory(), &value);
//	}
//}
//
extern "C" YBEHAVIOR_API YBehavior::TYPEID GetVariableTypeID(YBehavior::ISharedVariableEx* pVariable)
{
	return pVariable->TypeID();
}

extern "C" YBEHAVIOR_API YBehavior::TYPEID GetVariableElementTypeID(YBehavior::ISharedVariableEx* pVariable)
{
	return pVariable->ElementTypeID();
}
//
//#define VARIABLE_SIMPLETYPES_OPERATIONS(TYPE)\
//extern "C" YBEHAVIOR_API YBehavior::##TYPE GetVariable##TYPE(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::##TYPE>* pVariable)\
//{\
//	return GetVariableValue<YBehavior::##TYPE>(pAgent, pVariable);\
//}\
//extern "C" YBEHAVIOR_API void SetVariable##TYPE(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::##TYPE>* pVariable, YBehavior::##TYPE value)\
//{\
//	SetVariableValue<YBehavior::##TYPE>(pAgent, pVariable, value);\
//}
//
//VARIABLE_SIMPLETYPES_OPERATIONS(Int);
//VARIABLE_SIMPLETYPES_OPERATIONS(Ulong);
//VARIABLE_SIMPLETYPES_OPERATIONS(Float);
//VARIABLE_SIMPLETYPES_OPERATIONS(Bool);
//VARIABLE_SIMPLETYPES_OPERATIONS(Vector3);
//
////extern "C" YBEHAVIOR_API YBehavior::CSTRING GetVariableString(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::STRING>* pVariable)
////{
////	if (pVariable != nullptr && pAgent != nullptr)
////	{
////		const YBehavior::STRING* res = pVariable->GetCastedValue(pAgent->GetMemory());
////		return res ? res->c_str() : nullptr;
////	}
////	return nullptr;
////}
//extern "C" YBEHAVIOR_API bool GetVariableString(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::STRING>* pVariable, YBehavior::CSTRING outputStr, unsigned maxLength)
//{
//	if (pVariable != nullptr && pAgent != nullptr)
//	{
//		const YBehavior::STRING* res = pVariable->GetCastedValue(pAgent->GetMemory());
//		if (res)
//		{
//			strcpy_s(outputStr, maxLength, res->c_str());
//			return true;
//		}
//		return false;
//	}
//	return false;
//}
//
//extern "C" YBEHAVIOR_API void SetVariableString(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::STRING>* pVariable, YBehavior::CSTRING_CONST value)
//{
//	YBehavior::STRING s(value);
//	SetVariableValue<YBehavior::STRING>(pAgent, pVariable, s);
//}
//
//extern "C" YBEHAVIOR_API YBehavior::Entity* GetEntityFromWrapper(YBehavior::EntityWrapper* pWrapper)
//{
//	if (pWrapper && pWrapper->IsValid()) return pWrapper->Get();
//	
//	return nullptr;
//}
//
//extern "C" YBEHAVIOR_API YBehavior::Entity* GetEntityFromVariable(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::EntityWrapper>* pVariable)
//{
//	if (pVariable != nullptr && pAgent != nullptr)
//	{
//		const YBehavior::EntityWrapper* res = pVariable->GetCastedValue(pAgent->GetMemory());
//		if (res && res->IsValid())
//			return res->Get();
//		return nullptr;
//	}
//	return nullptr;
//}
//
//extern "C" YBEHAVIOR_API void SetEntityToVariable(YBehavior::Agent* pAgent, YBehavior::SharedVariableEx<YBehavior::EntityWrapper>* pVariable, YBehavior::Entity* pEntity)
//{
//	if (pVariable != nullptr && pAgent != nullptr && pEntity != nullptr)
//	{
//		YBehavior::EntityWrapper wrapper(pEntity->GetWrapper());
//		pVariable->SetCastedValue(pAgent->GetMemory(), &wrapper);
//	}
//}
