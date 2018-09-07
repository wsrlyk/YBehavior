#pragma once
#include "YBehavior/types.h"
#include "Ybehavior/agent.h"
#include "YBehavior/interface.h"

///> int GetIntVectorSize(VecInt* pVec)
///> void ClearIntVector(VecInt* pVec)
///> void PushBackIntVector(VecInt* pVec, int value)
///> void SetIntVectorAtIndex(VecInt* pVec, int index, int value)
///> int GetIntVectorAtIndex(VecInt* pVec, int index)

#define VECTOR_BASIC_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API int Get##TYPE##VectorSize(YBehavior::Vec##TYPE* pVec)\
{\
	return (int)pVec->size();\
}\
\
extern "C" YBEHAVIOR_API void Clear##TYPE##Vector(YBehavior::Vec##TYPE* pVec)\
{\
	pVec->clear();\
}

#define VECTOR_SIMPLETYPES_OPERATIONS(TYPE)\
extern "C" YBEHAVIOR_API void PushBack##TYPE##Vector(YBehavior::Vec##TYPE* pVec, YBehavior::##TYPE value)\
{\
	pVec->push_back(value);\
}\
extern "C" YBEHAVIOR_API void Set##TYPE##VectorAtIndex(YBehavior::Vec##TYPE* pVec, int index, YBehavior::##TYPE value)\
{\
	if (0 <= index && index < pVec->size())\
		(*pVec)[index] = value;\
}\
extern "C" YBEHAVIOR_API YBehavior::##TYPE Get##TYPE##VectorAtIndex(YBehavior::Vec##TYPE* pVec, int index)\
{\
	if (0 <= index && index < pVec->size())\
		return (*pVec)[index];\
}

VECTOR_BASIC_OPERATIONS(Int);
VECTOR_BASIC_OPERATIONS(Float);
VECTOR_BASIC_OPERATIONS(Ulong);
VECTOR_BASIC_OPERATIONS(Bool);
VECTOR_BASIC_OPERATIONS(EntityWrapper);
VECTOR_BASIC_OPERATIONS(Vector3);
VECTOR_BASIC_OPERATIONS(String);

VECTOR_SIMPLETYPES_OPERATIONS(Int);
VECTOR_SIMPLETYPES_OPERATIONS(Float);
VECTOR_SIMPLETYPES_OPERATIONS(Ulong);
VECTOR_SIMPLETYPES_OPERATIONS(Bool);
VECTOR_SIMPLETYPES_OPERATIONS(Vector3);

extern "C" YBEHAVIOR_API void PushBackEntityVector(YBehavior::VecEntityWrapper* pVec, YBehavior::Entity* pEntity)
{
	pVec->push_back(pEntity->CreateWrapper());
}

extern "C" YBEHAVIOR_API void SetEntityVectorAtIndex(YBehavior::VecEntityWrapper* pVec, int index, YBehavior::Entity* pEntity)
{
	if (0 <= index && index < pVec->size())
		(*pVec)[index] = pEntity->CreateWrapper();
}

extern "C" YBEHAVIOR_API YBehavior::Entity* GetEntityVectorAtIndex(YBehavior::VecEntityWrapper* pVec, int index)
{
	if (0 <= index && index < pVec->size())
		return ((*pVec)[index]).Get();
}


extern "C" YBEHAVIOR_API void PushBackStringVector(YBehavior::VecString* pVec, YBehavior::CSTRING value)
{
	pVec->push_back(value);
}

extern "C" YBEHAVIOR_API void SetStringVectorAtIndex(YBehavior::VecString* pVec, int index, YBehavior::CSTRING value)
{
	if (0 <= index && index < pVec->size())
		(*pVec)[index] = value;
}

extern "C" YBEHAVIOR_API YBehavior::CSTRING GetStringVectorAtIndex(YBehavior::VecString* pVec, int index)
{
	if (0 <= index && index < pVec->size())
		return ((*pVec)[index]).c_str();
}
