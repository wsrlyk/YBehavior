#ifndef _YBEHAVIOR_SHAREDDATA_H_
#define _YBEHAVIOR_SHAREDDATA_H_

#include "YBehavior/define.h"
#include <string>
#include <vector>

namespace YBehavior
{
	class Agent;
	typedef Agent* AgentPtr;
	typedef STRING				String;
	typedef INT					Int;
	typedef UINT				Uint;
	typedef BYTE				Byte;
	typedef BOOL				Bool;
	typedef FLOAT				Float;
	typedef std::vector<STRING>	VecString;
	typedef std::vector<INT>	VecInt;
	typedef std::vector<UINT>	VecUint;
	typedef std::vector<BYTE>	VecByte;
	typedef std::vector<BOOL>	VecBool;
	typedef std::vector<FLOAT>	VecFloat;
	typedef std::vector<AgentPtr>	VecAgentPtr;
	typedef std::vector<Vector3>	VecVector3;

	class YBEHAVIOR_API SharedData
	{
#define DECLARE_VARIABLES(T)			std::vector<T>* m_##T##s;	///> 定义变量
#define CONSTRUCT_VARIABLE(T)				m_##T##s = nullptr; 	///> 构造变量
#define DESTRUCT_VARIABLES(T)				if (m_##T##s != nullptr) delete m_##T##s; ///> 析构变量
#define DECLARE_DEFAULT_TYPES(T)		const static T s_Default##T;
#define DEFINE_DEFAULT_TYPES_V(T, V)	const T SharedData::s_Default##T(V);
///> 各种函数
#define DEFINE_TYPES_FUNC(T)\
	const T& Get##T(INT index)\
	{\
		if (m_##T##s == nullptr || index < 0 || index >= m_##T##s->size())\
			return s_Default##T;\
		return (*m_##T##s)[index];\
	}\
	\
	bool Set##T(INT index, const T& v)\
	{\
		if (index < 0)\
			return false;\
		if (m_##T##s == nullptr)\
			m_##T##s = new std::vector<T>(4);\
		if (index >= m_##T##s->size())\
		{\
			m_##T##s->reserve(m_##T##s->size() * 2);\
		}\
		(*m_##T##s)[index] = v;\
		return true;\
	}\
	\
	bool Set##T(INT index, T&& v)\
	{\
		if (index < 0)\
			return false;\
		if (m_##T##s == nullptr)\
			m_##T##s = new std::vector<T>(4);\
		if (index >= m_##T##s->size())\
		{\
			m_##T##s->reserve(m_##T##s->size() * 2);\
		}\
		(*m_##T##s)[index] = v;\
		return true;\
	}

#define FOR_EACH_TYPE(func)	\
	func(Int);	\
	func(Bool);	\
	func(Float);	\
	func(String);	\
	func(AgentPtr);	\
	func(Vector3);\
	func(VecInt);\
	func(VecBool);\
	func(VecFloat);\
	func(VecString);\
	func(VecAgentPtr);\
	func(VecVector3);
#define FOR_EACH_TYPE_WITH_VALUE(func)	\
	func(Int, 0);	\
	func(Bool, false);	\
	func(Float, 0.0f);	\
	func(String, "");	\
	func(AgentPtr, nullptr);	\
	func(Vector3, Vector3::zero);\
	func(VecInt, 1);\
	func(VecBool, 1);	\
	func(VecFloat, 1);	\
	func(VecString, 1);	\
	func(VecAgentPtr, 1);	\
	func(VecVector3, 1);	

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
	protected:
		FOR_EACH_TYPE(DECLARE_VARIABLES);
	public:
		FOR_EACH_TYPE(DECLARE_DEFAULT_TYPES);
		const static int INVALID_INDEX = -1;

	public:
		SharedData()
		{
			FOR_EACH_TYPE(CONSTRUCT_VARIABLE);
		}
		~SharedData()
		{
			FOR_EACH_TYPE(DESTRUCT_VARIABLES);
		}

		FOR_EACH_TYPE(DEFINE_TYPES_FUNC);
	};

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////

	template<typename T>
	class YBEHAVIOR_API SharedVariable
	{
	protected:
		T m_Value;
		INT m_Index;

		//const T& GetValue(SharedData* data) = 0;
		//bool SetValue(SharedData* data, const T& v) = 0;
	};

	
#define DECLARE_SHARED_TYPES(T)\
	class YBEHAVIOR_API Shared##T : public SharedVariable<T>\
	{\
	public:\
		Shared##T();\
		const T& GetValue(SharedData* data);\
		bool SetValue(SharedData* data, const T& v);\
		bool SetValue(SharedData* data, T&& v);\
	};\

	FOR_EACH_TYPE(DECLARE_SHARED_TYPES);
}

#endif